using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AgroTechApp.Models.DB;
using AgroTechApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace AgroTechApp.Controllers
{
    [Authorize]
    public class GastoesController : BaseController
    {
        public GastoesController(
            AgroTechDbContext context,
            ILogger<GastoesController> logger)
            : base(context, logger)
        {
        }

        // GET: Gastoes
        // En GastoesController.cs
        public async Task<IActionResult> Index(int? pagina)
        {
            var fincaId = GetFincaId();

            var query = _context.Gastos
                .Where(g => g.FincaId == fincaId)
                .Include(g => g.Animal)
                .Include(g => g.RubroGasto)
                .Include(g => g.Insumo)
                .AsQueryable();

            // Contar total
            var totalRegistros = await query.CountAsync();

            // Ordenar
            query = query.OrderByDescending(g => g.Fecha);

            // PAGINACIÓN
            int registrosPorPagina = 10;
            int paginaActual = pagina ?? 1;
            int totalPaginas = (int)Math.Ceiling(totalRegistros / (double)registrosPorPagina);

            var gastos = await query
                .Skip((paginaActual - 1) * registrosPorPagina)
                .Take(registrosPorPagina)
                .ToListAsync();

            // ViewBags
            ViewBag.PaginaActual = paginaActual;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalRegistros = totalRegistros;

            return View(gastos);
        }

        // GET: Gastoes/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var fincaId = GetFincaId();

                var gasto = await _context.Gastos
                    .Where(g => g.FincaId == fincaId && g.GastoId == id)
                    .Include(g => g.Animal)
                    .Include(g => g.Finca)
                    .Include(g => g.Insumo)
                    .Include(g => g.Potrero)
                    .Include(g => g.RubroGasto)
                    .FirstOrDefaultAsync();

                if (gasto == null)
                {
                    return NotFound();
                }

                return View(gasto);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // GET: Gastoes/Create
        public IActionResult Create()
        {
            try
            {
                var fincaId = GetFincaId();

                // ViewBag para dropdowns (requerido por las vistas)
                ViewBag.RubroGastoId = new SelectList(_context.RubroGastos, "RubroGastoId", "Nombre");
                ViewBag.AnimalId = new SelectList(
                    _context.Animals
                        .Where(a => a.FincaId == fincaId && a.Estado == "Activo")
                        .OrderBy(a => a.Arete)
                        .ToList(),
                    "AnimalId",
                    "Arete");
                ViewBag.InsumoId = new SelectList(
                    _context.Insumos
                        .Where(i => i.FincaId == fincaId && i.Activo == true)
                        .OrderBy(i => i.Nombre)
                        .ToList(),
                    "InsumoId",
                    "Nombre");

                return View();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // POST: Gastoes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GastoId,RubroGastoId,Fecha,Monto,Descripcion,AnimalId,PotreroId,InsumoId")] Gasto gasto)
        {
            try
            {
                var fincaId = GetFincaId();
                gasto.FincaId = fincaId;

                // Los gastos creados manualmente NO son automáticos
                gasto.EsAutomatico = false;
                gasto.OrigenModulo = FinanzasConstants.OrigenModulos.MANUAL;
                gasto.ReferenciaOrigenId = null;

                ModelState.Remove("FincaId");
                ModelState.Remove("Finca");
                ModelState.Remove("Animal");
                ModelState.Remove("Insumo");
                ModelState.Remove("Potrero");
                ModelState.Remove("RubroGasto");

                // Validar que entidades relacionadas pertenecen a la finca
                if (gasto.AnimalId.HasValue)
                {
                    var animal = await _context.Animals
                        .FirstOrDefaultAsync(a => a.AnimalId == gasto.AnimalId && a.FincaId == fincaId);
                    if (animal == null)
                    {
                        ModelState.AddModelError("AnimalId", "El animal no pertenece a su finca");
                    }
                }

                if (gasto.InsumoId.HasValue)
                {
                    var insumo = await _context.Insumos
                        .FirstOrDefaultAsync(i => i.InsumoId == gasto.InsumoId && i.FincaId == fincaId);
                    if (insumo == null)
                    {
                        ModelState.AddModelError("InsumoId", "El insumo no pertenece a su finca");
                    }
                }

                if (gasto.PotreroId.HasValue)
                {
                    var potrero = await _context.Potreros
                        .FirstOrDefaultAsync(p => p.PotreroId == gasto.PotreroId && p.FincaId == fincaId);
                    if (potrero == null)
                    {
                        ModelState.AddModelError("PotreroId", "El potrero no pertenece a su finca");
                    }
                }

                if (ModelState.IsValid)
                {
                    _context.Add(gasto);
                    await _context.SaveChangesAsync();
                    MostrarExito("Gasto registrado exitosamente");
                    return RedirectToAction(nameof(Index));
                }

                // Recargar dropdowns
                ViewBag.RubroGastoId = new SelectList(_context.RubroGastos, "RubroGastoId", "Nombre", gasto.RubroGastoId);
                ViewBag.AnimalId = new SelectList(
                    _context.Animals
                        .Where(a => a.FincaId == fincaId && a.Estado == "Activo") 
                        .OrderBy(a => a.Arete)
                        .ToList(),
                    "AnimalId", "Arete", gasto.AnimalId);

                ViewBag.InsumoId = new SelectList(
                    _context.Insumos
                        .Where(i => i.FincaId == fincaId && i.Activo == true)  
                        .OrderBy(i => i.Nombre)
                        .ToList(),
                    "InsumoId", "Nombre", gasto.InsumoId);

                return View(gasto);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // GET: Gastoes/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var fincaId = GetFincaId();

                var gasto = await _context.Gastos
                    .Where(g => g.FincaId == fincaId && g.GastoId == id)
                    .FirstOrDefaultAsync();

                if (gasto == null) return NotFound();

                // No permitir editar registros automáticos
                if (gasto.EsAutomatico)
                {
                    MostrarError("No se pueden editar registros automáticos generados por el sistema.");
                    return RedirectToAction(nameof(Details), new { id });
                }

                ViewBag.RubroGastoId = new SelectList(
                    _context.RubroGastos,
                    "RubroGastoId",
                    "Nombre",
                    gasto.RubroGastoId);

                // SOLO ANIMALES ACTIVOS
                ViewBag.AnimalId = new SelectList(
                    _context.Animals
                        .Where(a => a.FincaId == fincaId && a.Estado == "Activo")
                        .OrderBy(a => a.Arete)
                        .ToList(),
                    "AnimalId",
                    "Arete",
                    gasto.AnimalId);

                ViewBag.PotreroId = new SelectList(
                    _context.Potreros
                        .Where(p => p.FincaId == fincaId)
                        .OrderBy(p => p.Nombre)
                        .ToList(),
                    "PotreroId",
                    "Nombre",
                    gasto.PotreroId);

                // SOLO INSUMOS ACTIVOS
                ViewBag.InsumoId = new SelectList(
                    _context.Insumos
                        .Where(i => i.FincaId == fincaId && i.Activo == true)
                        .OrderBy(i => i.Nombre)
                        .ToList(),
                    "InsumoId",
                    "Nombre",
                    gasto.InsumoId);

                return View(gasto);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // POST: Gastoes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("GastoId,FincaId,RubroGastoId,Fecha,Monto,Descripcion,AnimalId,PotreroId,InsumoId")] Gasto gasto)
        {
            if (id != gasto.GastoId)
            {
                return NotFound();
            }

            try
            {
                var fincaId = GetFincaId();

                // Obtener el gasto original para verificar si es automático
                var gastoOriginal = await _context.Gastos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.GastoId == id && g.FincaId == fincaId);

                if (gastoOriginal == null)
                {
                    return NotFound();
                }

                // BLOQUEAR EDICION DE REGISTROS AUTOMATICOS
                if (gastoOriginal.EsAutomatico)
                {
                    MostrarError("No se pueden editar registros automáticos generados por el sistema.");
                    return RedirectToAction(nameof(Details), new { id });
                }

                ModelState.Remove("FincaId");
                ModelState.Remove("Finca");
                ModelState.Remove("Animal");
                ModelState.Remove("Insumo");
                ModelState.Remove("Potrero");
                ModelState.Remove("RubroGasto");

                ValidarAcceso(gasto.FincaId);

                if (ModelState.IsValid)
                {
                    try
                    {
                        //PRESERVAR campos de automatización
                        gasto.EsAutomatico = gastoOriginal.EsAutomatico;
                        gasto.OrigenModulo = gastoOriginal.OrigenModulo;
                        gasto.ReferenciaOrigenId = gastoOriginal.ReferenciaOrigenId;

                        _context.Update(gasto);
                        await _context.SaveChangesAsync();
                        MostrarExito("Gasto actualizado exitosamente");
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!GastoExists(gasto.GastoId))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.RubroGastoId = new SelectList(_context.RubroGastos, "RubroGastoId", "Nombre", gasto.RubroGastoId);
                ViewBag.AnimalId = new SelectList(
                    _context.Animals.Where(a => a.FincaId == fincaId),
                    "AnimalId", "Arete", gasto.AnimalId);
                ViewBag.PotreroId = new SelectList(
                    _context.Potreros.Where(p => p.FincaId == fincaId),
                    "PotreroId", "Nombre", gasto.PotreroId);
                ViewBag.InsumoId = new SelectList(
                    _context.Insumos.Where(i => i.FincaId == fincaId),
                    "InsumoId", "Nombre", gasto.InsumoId);

                return View(gasto);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // GET: Gastoes/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var fincaId = GetFincaId();

                var gasto = await _context.Gastos
                    .Where(g => g.FincaId == fincaId && g.GastoId == id)
                    .Include(g => g.Animal)
                    .Include(g => g.Finca)
                    .Include(g => g.Insumo)
                    .Include(g => g.Potrero)
                    .Include(g => g.RubroGasto)
                    .FirstOrDefaultAsync();

                if (gasto == null)
                {
                    return NotFound();
                }

                // ADVERTENCIA SI ES AUTOMATICO
                if (gasto.EsAutomatico)
                {
                    ViewBag.EsAutomatico = true;
                    ViewBag.OrigenModulo = gasto.OrigenModulo;
                }

                return View(gasto);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // POST: Gastoes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var fincaId = GetFincaId();

                var gasto = await _context.Gastos
                    .Where(g => g.FincaId == fincaId && g.GastoId == id)
                    .FirstOrDefaultAsync();

                if (gasto == null)
                {
                    return NotFound();
                }

                // NO PERMITIR ELIMINAR REGISTROS AUTOMATICOS
                if (gasto.EsAutomatico)
                {
                    MostrarError($"No se pueden eliminar registros automáticos. Este gasto fue generado por el módulo: {gasto.OrigenModulo}. Use el módulo correspondiente para revertir la operación.");
                    return RedirectToAction(nameof(Index));
                }

                _context.Gastos.Remove(gasto);
                await _context.SaveChangesAsync();
                MostrarExito("Gasto eliminado exitosamente");
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        private bool GastoExists(long id)
        {
            return _context.Gastos.Any(e => e.GastoId == id);
        }
    }
}