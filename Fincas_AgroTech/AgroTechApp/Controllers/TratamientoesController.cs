using AgroTechApp.Models;
using AgroTechApp.Models.DB;
using AgroTechApp.Services;
using AgroTechApp.Services.Inventario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AgroTechApp.Controllers
{
    [Authorize]
    public class TratamientoesController : BaseController
    {
        private readonly IFinanzasService _finanzasService;
        private readonly IInventarioService _inventarioService;

        public TratamientoesController(
            AgroTechDbContext context,
            IFinanzasService finanzasService,
            IInventarioService inventarioService,
            ILogger<TratamientoesController> logger)
            : base(context, logger)
        {
            _finanzasService = finanzasService;
            _inventarioService = inventarioService;
        }

        // ============================================================
        // GET: Tratamientos (solo finca del usuario)
        // ============================================================
        public async Task<IActionResult> Index(int? pagina)
        {
            try
            {
                long fincaId = GetFincaId();

                var query = _context.Tratamientos
                    .Where(t => t.FincaId == fincaId)      // MULTI-TENANT
                    .Include(t => t.Animal)
                    .Include(t => t.TipoTrat)
                    .Include(t => t.Insumo)
                    .Include(t => t.Lote)
                    .Include(t => t.LoteAnimal)
                    .AsQueryable();

                // Contar total antes de paginar
                var totalRegistros = await query.CountAsync();

                // Más recientes primero (por TratamientoId descendente)
                query = query.OrderByDescending(t => t.TratamientoId);

                // PAGINACIÓN
                int registrosPorPagina = 10;
                int paginaActual = pagina ?? 1;
                int totalPaginas = (int)Math.Ceiling(totalRegistros / (double)registrosPorPagina);

                var tratamientos = await query
                    .Skip((paginaActual - 1) * registrosPorPagina)
                    .Take(registrosPorPagina)
                    .ToListAsync();

                // ViewBags para la vista
                ViewBag.PaginaActual = paginaActual;
                ViewBag.TotalPaginas = totalPaginas;
                ViewBag.TotalRegistros = totalRegistros;

                return View(tratamientos);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================================================
        // GET: Details
        // ============================================================
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                long fincaId = GetFincaId();

                var tratamiento = await _context.Tratamientos
                    .Include(t => t.Animal)
                    .Include(t => t.TipoTrat)
                    .Include(t => t.Insumo)
                        .ThenInclude(i => i.Unidad)
                    .Include(t => t.Lote)
                    .Include(t => t.LoteAnimal)
                    .FirstOrDefaultAsync(t => t.TratamientoId == id && t.FincaId == fincaId);

                if (tratamiento == null) return NotFound();

                return View(tratamiento);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================================================
        // GET: Create
        // ============================================================
        public async Task<IActionResult> Create()
        {
            try
            {
                long fincaId = GetFincaId();
                await CargarCombos(fincaId);
                return View();
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================================================
        // POST: Create
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Tratamiento t)
        {
            try
            {
                long fincaId = GetFincaId();
                t.FincaId = fincaId; // 🔒 No permitir manipular finca desde el cliente

                // Remover validaciones de navegación
                ModelState.Remove("FincaId");
                ModelState.Remove("Finca");
                ModelState.Remove("Animal");
                ModelState.Remove("Insumo");
                ModelState.Remove("TipoTrat");
                ModelState.Remove("LoteAnimal");
                ModelState.Remove("Lote");

                // 🔒 VALIDAR ANIMAL
                if (!await _context.Animals.AnyAsync(a => a.AnimalId == t.AnimalId && a.FincaId == fincaId))
                {
                    ModelState.AddModelError("AnimalId", "El animal no pertenece a su finca.");
                    _logger.LogWarning($"Intento de crear tratamiento con animal {t.AnimalId} que no pertenece a finca {fincaId}");
                }

                // 🔒 VALIDAR INSUMO (si está presente)
                if (t.InsumoId.HasValue &&
                    !await _context.Insumos.AnyAsync(i => i.InsumoId == t.InsumoId && i.FincaId == fincaId))
                {
                    ModelState.AddModelError("InsumoId", "El insumo no pertenece a su finca.");
                    _logger.LogWarning($"Intento de crear tratamiento con insumo {t.InsumoId} que no pertenece a finca {fincaId}");
                }

                // 🔒 VALIDAR STOCK disponible si hay insumo Y dosis
                if (t.InsumoId.HasValue && !string.IsNullOrWhiteSpace(t.Dosis))
                {
                    // Intentar parsear la cantidad de la dosis
                    if (decimal.TryParse(t.Dosis.Split(' ')[0], out decimal cantidad))
                    {
                        var stockDict = await _inventarioService.GetStockPorInsumoAsync(fincaId, null);
                        var stockDisponible = stockDict.TryGetValue(t.InsumoId.Value, out var stock) ? stock : 0m;

                        if (cantidad > stockDisponible)
                        {
                            var insumo = await _context.Insumos
                                .Include(i => i.Unidad)
                                .FirstOrDefaultAsync(i => i.InsumoId == t.InsumoId.Value);

                            ModelState.AddModelError("Dosis",
                                $"Stock insuficiente. Disponible: {stockDisponible:N2} {insumo?.Unidad?.Codigo ?? "unidad"}");
                        }
                    }
                }

                // 🔒 VALIDAR LOTE DE INSUMO (si está presente)
                if (t.LoteId.HasValue &&
                    !await _context.InsumoLotes
                        .Include(l => l.Insumo)
                        .AnyAsync(l => l.LoteId == t.LoteId && l.Insumo.FincaId == fincaId))
                {
                    ModelState.AddModelError("LoteId", "El lote no pertenece a su finca.");
                }

                // 🔒 VALIDAR LOTE ANIMAL (si está presente)
                if (t.LoteAnimalId.HasValue &&
                    !await _context.LoteAnimals.AnyAsync(l => l.LoteAnimalId == t.LoteAnimalId && l.FincaId == fincaId))
                {
                    ModelState.AddModelError("LoteAnimalId", "El lote animal no pertenece a su finca.");
                }

                if (!ModelState.IsValid)
                {
                    await CargarCombos(fincaId);
                    return View(t);
                }

                using var tx = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Crear el tratamiento
                    _context.Add(t);
                    await _context.SaveChangesAsync();

                    // SI HAY INSUMO Y DOSIS → Procesar consumo y gasto solo entrar aquí si REALMENTE hay insumo
                    if (t.InsumoId.HasValue && !string.IsNullOrWhiteSpace(t.Dosis))
                    {
                        // Parsear cantidad de la dosis
                        if (decimal.TryParse(t.Dosis.Split(' ')[0], out decimal cantidad) && cantidad > 0)
                        {
                            var insumo = await _context.Insumos
                                .Include(i => i.Unidad)
                                .FirstOrDefaultAsync(i => i.InsumoId == t.InsumoId.Value);

                            if (insumo != null)
                            {
                                // REGISTRAR CONSUMO DE INVENTARIO
                                await _inventarioService.RegistrarConsumoAsync(
                                    fincaId: fincaId,
                                    insumoId: t.InsumoId.Value,
                                    cantidad: cantidad,
                                    loteId: t.LoteId,
                                    observacion: $"Tratamiento: {t.TipoTrat?.Nombre ?? "N/A"} - Animal: {t.Animal?.Arete ?? "N/A"}",
                                    fecha: t.Fecha
                                );

                                // CALCULAR COSTO Y REGISTRAR GASTO
                                var costoPromedio = await _finanzasService.CalcularCostoPromedioInsumo(t.InsumoId.Value);
                                var costoTotal = cantidad * costoPromedio;

                                var tipoTrat = await _context.TipoTratamientos
                                    .FirstOrDefaultAsync(tt => tt.TipoTratId == t.TipoTratId);

                                await _finanzasService.RegistrarGastoTratamiento(
                                    fincaId: fincaId,
                                    tratamientoId: t.TratamientoId,
                                    animalId: t.AnimalId,
                                    insumoId: t.InsumoId,
                                    nombreInsumo: insumo.Nombre,
                                    tipoTratamiento: tipoTrat?.Nombre ?? "Tratamiento",
                                    costoTratamiento: costoTotal,
                                    fecha: t.Fecha
                                );

                                _logger.LogInformation(
                                    "Tratamiento {TratamientoId} con insumo: Consumo e inventario actualizados. Costo: {Costo:C}",
                                    t.TratamientoId, costoTotal);
                            }
                        }
                    }
                    else
                    {
                        // TRATAMIENTO SIN INSUMO - No se registra gasto
                        _logger.LogInformation(
                            "Tratamiento {TratamientoId} registrado SIN INSUMO. No se generó gasto.",
                            t.TratamientoId);
                    }

                    await tx.CommitAsync();

                    // Mensaje diferenciado según si hubo insumo o no
                    if (t.InsumoId.HasValue)
                    {
                        MostrarExito("Tratamiento registrado. Inventario y gasto actualizados automáticamente.");
                    }
                    else
                    {
                        MostrarExito("Tratamiento registrado correctamente (sin insumo asociado).");
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    _logger.LogError(ex, "Error al registrar tratamiento {TratamientoId}", t.TratamientoId);
                    ModelState.AddModelError(string.Empty, "Ocurrió un error al registrar el tratamiento.");
                    await CargarCombos(fincaId);
                    return View(t);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado en Tratamientos/Create");
                MostrarError("Debe iniciar sesión para acceder.");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================================================
        // GET: Edit
        // ============================================================
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                long fincaId = GetFincaId();

                var tratamiento = await _context.Tratamientos
                    .FirstOrDefaultAsync(t => t.TratamientoId == id && t.FincaId == fincaId);

                if (tratamiento == null) return NotFound();

                await CargarCombos(fincaId);
                return View(tratamiento);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================================================
        // POST: Edit
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, Tratamiento t)
        {
            if (id != t.TratamientoId) return NotFound();

            try
            {
                long fincaId = GetFincaId();

                // Validación estricta multi-tenant
                var original = await _context.Tratamientos
                    .FirstOrDefaultAsync(o => o.TratamientoId == id && o.FincaId == fincaId);

                if (original == null)
                    return Unauthorized();

                // ⚠️ NOTA: NO permitimos cambiar el insumo/cantidad en Edit
                // Para evitar inconsistencias en inventario y gastos
                // Si se equivocó, debe eliminar y crear nuevo

                // Validaciones igual que en Create:
                if (!await _context.Animals.AnyAsync(a => a.AnimalId == t.AnimalId && a.FincaId == fincaId))
                    ModelState.AddModelError("AnimalId", "El animal no pertenece a su finca.");

                if (t.InsumoId.HasValue &&
                    !await _context.Insumos.AnyAsync(i => i.InsumoId == t.InsumoId && i.FincaId == fincaId))
                    ModelState.AddModelError("InsumoId", "El insumo no pertenece a su finca.");

                if (t.LoteId.HasValue &&
                    !await _context.InsumoLotes.Include(l => l.Insumo)
                        .AnyAsync(l => l.LoteId == t.LoteId && l.Insumo.FincaId == fincaId))
                    ModelState.AddModelError("LoteId", "El lote no pertenece a su finca.");

                if (t.LoteAnimalId.HasValue &&
                    !await _context.LoteAnimals.AnyAsync(l => l.LoteAnimalId == t.LoteAnimalId && l.FincaId == fincaId))
                    ModelState.AddModelError("LoteAnimalId", "El lote animal no pertenece a su finca.");

                if (!ModelState.IsValid)
                {
                    await CargarCombos(fincaId);
                    return View(t);
                }

                // ACTUALIZAR solamente campos editables (NO insumo ni dosis)
                original.AnimalId = t.AnimalId;
                original.TipoTratId = t.TipoTratId;
                original.LoteAnimalId = t.LoteAnimalId;
                original.Fecha = t.Fecha;
                // original.InsumoId = NO EDITABLE
                // original.Dosis = NO EDITABLE
                original.Via = t.Via;
                original.Responsable = t.Responsable;
                original.Observacion = t.Observacion;

                await _context.SaveChangesAsync();
                MostrarExito("Tratamiento actualizado correctamente.");

                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================================================
        // GET: Delete
        // ============================================================
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                long fincaId = GetFincaId();

                var tratamiento = await _context.Tratamientos
                    .Include(t => t.Animal)
                    .Include(t => t.TipoTrat)
                    .Include(t => t.Insumo)
                    .FirstOrDefaultAsync(t => t.TratamientoId == id && t.FincaId == fincaId);

                if (tratamiento == null)
                    return NotFound();

                return View(tratamiento);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================================================
        // POST: DeleteConfirmed
        // ============================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                long fincaId = GetFincaId();

                var tratamiento = await _context.Tratamientos
                    .FirstOrDefaultAsync(t => t.TratamientoId == id && t.FincaId == fincaId);

                if (tratamiento == null)
                    return NotFound();

                using var tx = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Eliminar gasto automático asociado (si existe)
                    await _finanzasService.EliminarGastoDeTratamiento(id);

                    // Eliminar el tratamiento
                    _context.Tratamientos.Remove(tratamiento);
                    await _context.SaveChangesAsync();

                    await tx.CommitAsync();

                    MostrarExito("Tratamiento y gasto asociado eliminados correctamente.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    _logger.LogError(ex, "Error al eliminar tratamiento");
                    ErrorView("Ocurrió un error al eliminar el tratamiento.");
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================================================
        // AJAX: Obtener stock de insumo
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> ObtenerStockInsumo(long insumoId)
        {
            try
            {
                long fincaId = GetFincaId();

                var insumo = await _context.Insumos
                    .Include(i => i.Unidad)
                    .FirstOrDefaultAsync(i => i.InsumoId == insumoId && i.FincaId == fincaId);

                if (insumo == null)
                    return Json(new { success = false, message = "Insumo no encontrado" });

                var stockDict = await _inventarioService.GetStockPorInsumoAsync(fincaId, null);
                var stock = stockDict.TryGetValue(insumoId, out var s) ? s : 0m;

                return Json(new
                {
                    success = true,
                    stock = stock,
                    unidad = insumo.Unidad?.Codigo ?? insumo.Unidad?.Nombre ?? "unidad",
                    nombreInsumo = insumo.Nombre
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener stock del insumo {InsumoId}", insumoId);
                return Json(new { success = false, message = "Error al obtener stock" });
            }
        }

        // ============================================================
        // HELPERS
        // ============================================================
        private async Task CargarCombos(long fincaId)
        {
            // Solo activos, ordenados
            ViewBag.AnimalId = new SelectList(
                await _context.Animals
                    .Where(a => a.FincaId == fincaId &&
                           a.Estado != "Vendido" &&
                           a.Estado != "Muerto")
                    .OrderBy(a => a.Arete)
                    .Select(a => new { a.AnimalId, Texto = a.Arete + " - " + a.Nombre })
                    .ToListAsync(),
                "AnimalId", "Texto"
            );

            ViewBag.TipoTratId = new SelectList(
                await _context.TipoTratamientos.OrderBy(t => t.Nombre).ToListAsync(),
                "TipoTratId", "Nombre"
            );

            ViewBag.InsumoId = new SelectList(
                await _context.Insumos
                    .Where(i => i.FincaId == fincaId)
                    .OrderBy(i => i.Nombre)
                    .Select(i => new { i.InsumoId, Texto = i.Nombre })
                    .ToListAsync(),
                "InsumoId", "Texto"
            );

            ViewBag.LoteId = new SelectList(
                await _context.InsumoLotes
                    .Include(l => l.Insumo)
                    .Where(l => l.Insumo.FincaId == fincaId)
                    .OrderBy(l => l.CodigoLote)
                    .Select(l => new { l.LoteId, Texto = l.CodigoLote })
                    .ToListAsync(),
                "LoteId", "Texto"
            );

            ViewBag.LoteAnimalId = new SelectList(
                await _context.LoteAnimals
                    .Where(l => l.FincaId == fincaId)
                    .OrderBy(l => l.Nombre)
                    .Select(l => new { l.LoteAnimalId, Texto = l.Nombre })
                    .ToListAsync(),
                "LoteAnimalId", "Texto"
            );
        }
    }
}