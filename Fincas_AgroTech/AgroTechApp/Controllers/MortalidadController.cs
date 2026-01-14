using AgroTechApp.Models;
using AgroTechApp.Models.DB;
using AgroTechApp.Services;
using AgroTechApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AgroTechApp.Controllers
{
    [Authorize]
    public class MortalidadController : BaseController
    {
        private readonly IFinanzasService _finanzasService;

        public MortalidadController(
            AgroTechDbContext context,
            IFinanzasService finanzasService,
            ILogger<MortalidadController> logger)
            : base(context, logger)
        {
            _finanzasService = finanzasService;
        }

        // ============================================================
        // GET: Mortalidad/Index
        // ============================================================
        public async Task<IActionResult> Index(int? pagina)
        {
            try
            {
                long fincaId = GetFincaId();

                var query = _context.Mortalidads
                    .Include(m => m.Animal)
                        .ThenInclude(a => a.Raza)
                    .Where(m => m.Animal.FincaId == fincaId)
                    .AsQueryable();

                // Contar total
                var totalRegistros = await query.CountAsync();

                // Ordenar: Más recientes primero
                query = query.OrderByDescending(m => m.MortalidadId);

                // Paginación
                int registrosPorPagina = 10;
                int paginaActual = pagina ?? 1;
                int totalPaginas = (int)Math.Ceiling(totalRegistros / (double)registrosPorPagina);

                var mortalidades = await query
                    .Skip((paginaActual - 1) * registrosPorPagina)
                    .Take(registrosPorPagina)
                    .ToListAsync();

                // Calcular total de pérdidas
                var totalPerdidas = await _context.Gastos
                    .Where(g => g.FincaId == fincaId &&
                                g.OrigenModulo == FinanzasConstants.OrigenModulos.MORTALIDAD)
                    .SumAsync(g => (decimal?)g.Monto) ?? 0;

                ViewBag.TotalPerdidas = totalPerdidas;
                ViewBag.TotalMortalidades = totalRegistros;
                ViewBag.PaginaActual = paginaActual;
                ViewBag.TotalPaginas = totalPaginas;
                ViewBag.TotalRegistros = totalRegistros;

                return View(mortalidades);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================================================
        // GET: Mortalidad/Details/5
        // ============================================================
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                long fincaId = GetFincaId();

                var mortalidad = await _context.Mortalidads
                    .Include(m => m.Animal)
                        .ThenInclude(a => a.Raza)
                    .Where(m => m.MortalidadId == id && m.Animal.FincaId == fincaId)
                    .FirstOrDefaultAsync();

                if (mortalidad == null)
                {
                    return NotFound();
                }

                // Buscar el gasto de pérdida asociado
                var gastoPerdida = await _context.Gastos
                    .Include(g => g.RubroGasto)
                    .FirstOrDefaultAsync(g =>
                        g.OrigenModulo == "Mortalidad" &&
                        g.ReferenciaOrigenId == id);

                ViewBag.GastoPerdida = gastoPerdida;

                return View(mortalidad);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================================================
        // GET: Mortalidad/Create
        // ============================================================
        public async Task<IActionResult> Create()
        {
            try
            {
                long fincaId = GetFincaId();

                // Solo animales activos
                var animales = await _context.Animals
                    .Where(a => a.FincaId == fincaId && a.Estado == "Activo")
                    .OrderBy(a => a.Arete)
                    .Select(a => new
                    {
                        a.AnimalId,
                        Texto = a.Arete + (string.IsNullOrWhiteSpace(a.Nombre) ? "" : " - " + a.Nombre),
                        a.CostoCompra
                    })
                    .ToListAsync();

                if (!animales.Any())
                {
                    MostrarError("No hay animales activos para registrar mortalidad.");
                    return RedirectToAction(nameof(Index));
                }

                ViewData["AnimalId"] = new SelectList(animales, "AnimalId", "Texto");

                return View(new Mortalidad { Fecha = DateTime.Now });
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================================================
        // POST: Mortalidad/Create
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AnimalId,Fecha,Causa,Observacion")] Mortalidad mortalidad)
        {
            try
            {
                long fincaId = GetFincaId();

                ModelState.Remove("Animal");

                // Validar que el animal pertenece a la finca y está activo
                var animal = await _context.Animals
                    .Where(a => a.AnimalId == mortalidad.AnimalId && a.FincaId == fincaId)
                    .FirstOrDefaultAsync();

                if (animal == null)
                {
                    ModelState.AddModelError("AnimalId", "El animal no pertenece a su finca.");
                }
                else if (animal.Estado != "Activo")
                {
                    ModelState.AddModelError("AnimalId", $"El animal ya tiene estado '{animal.Estado}'.");
                }

                if (!ModelState.IsValid)
                {
                    await CargarAnimales(fincaId);
                    return View(mortalidad);
                }

                using var tx = await _context.Database.BeginTransactionAsync();

                try
                {
                    // ✅ PASO 1: Crear registro de mortalidad
                    _context.Mortalidads.Add(mortalidad);
                    await _context.SaveChangesAsync();

                    // ✅ PASO 2: Actualizar estado del animal
                    animal.Estado = "Muerto";
                    _context.Update(animal);
                    await _context.SaveChangesAsync();

                    // ✅ PASO 3: Registrar pérdida financiera (solo si tiene costo de compra)
                    decimal perdida = 0;
                    if (animal.CostoCompra.HasValue && animal.CostoCompra.Value > 0)
                    {
                        perdida = animal.CostoCompra.Value;

                        var rubroId = await _finanzasService.ObtenerIdRubroGasto(FinanzasConstants.RubrosGasto.PERDIDAS_MORTALIDAD);

                        if (rubroId.HasValue)
                        {
                            string nombreAnimal = string.IsNullOrWhiteSpace(animal.Nombre)
                                ? animal.Arete
                                : $"{animal.Arete} - {animal.Nombre}";

                            var gasto = new Gasto
                            {
                                FincaId = fincaId,
                                RubroGastoId = rubroId.Value,
                                Fecha = DateOnly.FromDateTime(mortalidad.Fecha),
                                Monto = perdida,
                                Descripcion = $"Pérdida por muerte de {nombreAnimal} - Causa: {mortalidad.Causa ?? "Desconocida"}",
                                AnimalId = animal.AnimalId,
                                EsAutomatico = true,
                                OrigenModulo = FinanzasConstants.OrigenModulos.MORTALIDAD,
                                ReferenciaOrigenId = mortalidad.MortalidadId
                            };

                            _context.Gastos.Add(gasto);
                            await _context.SaveChangesAsync();

                            _logger.LogInformation(
                                "Pérdida registrada por mortalidad: Animal {AnimalId}, Monto: {Monto}",
                                animal.AnimalId, perdida);
                        }
                    }

                    await tx.CommitAsync();

                    if (perdida > 0)
                    {
                        MostrarExito($"Mortalidad registrada. Pérdida de ₡{perdida:N2} registrada automáticamente.");
                    }
                    else
                    {
                        MostrarExito("Mortalidad registrada correctamente.");
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    _logger.LogError(ex, "Error al registrar mortalidad");
                    ModelState.AddModelError(string.Empty, "Ocurrió un error al registrar la mortalidad.");
                    await CargarAnimales(fincaId);
                    return View(mortalidad);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================================================
        // GET: Mortalidad/Delete/5
        // ============================================================
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                long fincaId = GetFincaId();

                var mortalidad = await _context.Mortalidads
                    .Include(m => m.Animal)
                        .ThenInclude(a => a.Raza)
                    .Where(m => m.MortalidadId == id && m.Animal.FincaId == fincaId)
                    .FirstOrDefaultAsync();

                if (mortalidad == null)
                {
                    return NotFound();
                }

                // Buscar gasto asociado
                var gastoAsociado = await _context.Gastos
                    .FirstOrDefaultAsync(g =>
                        g.OrigenModulo == FinanzasConstants.OrigenModulos.MORTALIDAD &&
                        g.ReferenciaOrigenId == id);

                ViewBag.TieneGasto = gastoAsociado != null;
                ViewBag.MontoGasto = gastoAsociado?.Monto ?? 0;

                return View(mortalidad);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================================================
        // POST: Mortalidad/Delete/5
        // ============================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                long fincaId = GetFincaId();

                var mortalidad = await _context.Mortalidads
                    .Include(m => m.Animal)
                    .Where(m => m.MortalidadId == id && m.Animal.FincaId == fincaId)
                    .FirstOrDefaultAsync();

                if (mortalidad == null)
                {
                    return NotFound();
                }

                using var tx = await _context.Database.BeginTransactionAsync();

                try
                {
                    //Revertir estado del animal
                    mortalidad.Animal.Estado = "Activo";
                    _context.Update(mortalidad.Animal);

                    //Eliminar gasto de pérdida si existe
                    var gastoAsociado = await _context.Gastos
                        .FirstOrDefaultAsync(g =>
                            g.OrigenModulo == FinanzasConstants.OrigenModulos.MORTALIDAD &&
                            g.ReferenciaOrigenId == id);

                    if (gastoAsociado != null)
                    {
                        _context.Gastos.Remove(gastoAsociado);
                    }

                    //Eliminar registro de mortalidad
                    _context.Mortalidads.Remove(mortalidad);

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    MostrarExito("Mortalidad eliminada. El animal ha sido reactivado.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    _logger.LogError(ex, "Error al eliminar mortalidad");
                    MostrarError("Ocurrió un error al eliminar la mortalidad.");
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================================================
        // AJAX: Obtener costo de compra del animal
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> ObtenerCostoAnimal(long animalId)
        {
            try
            {
                long fincaId = GetFincaId();

                var animal = await _context.Animals
                    .Where(a => a.AnimalId == animalId && a.FincaId == fincaId)
                    .Select(a => new
                    {
                        a.AnimalId,
                        a.Arete,
                        a.Nombre,
                        a.CostoCompra,
                        Raza = a.Raza != null ? a.Raza.Nombre : "N/A"
                    })
                    .FirstOrDefaultAsync();

                if (animal == null)
                {
                    return Json(new { success = false, message = "Animal no encontrado" });
                }

                return Json(new
                {
                    success = true,
                    costoCompra = animal.CostoCompra ?? 0,
                    arete = animal.Arete,
                    nombre = animal.Nombre ?? "",
                    raza = animal.Raza
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener costo del animal {AnimalId}", animalId);
                return Json(new { success = false, message = "Error al obtener información" });
            }
        }

        // ============================================================
        // HELPERS
        // ============================================================
        private async Task CargarAnimales(long fincaId)
        {
            var animales = await _context.Animals
                .Where(a => a.FincaId == fincaId && a.Estado == "Activo")
                .OrderBy(a => a.Arete)
                .Select(a => new
                {
                    a.AnimalId,
                    Texto = a.Arete + (string.IsNullOrWhiteSpace(a.Nombre) ? "" : " - " + a.Nombre)
                })
                .ToListAsync();

            ViewData["AnimalId"] = new SelectList(animales, "AnimalId", "Texto");
        }
    }
}