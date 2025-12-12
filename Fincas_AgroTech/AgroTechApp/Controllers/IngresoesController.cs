using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using AgroTechApp.Models.DB;
using AgroTechApp.Models;

namespace AgroTechApp.Controllers
{
    [Authorize]
    public class IngresoesController : BaseController
    {
        public IngresoesController(
            AgroTechDbContext context,
            ILogger<IngresoesController> logger)
            : base(context, logger)
        {
        }

        // GET: Ingresoes
        public async Task<IActionResult> Index()
        {
            try
            {
                var fincaId = GetFincaId();

                var ingresos = await _context.Ingresos
                    .Where(i => i.FincaId == fincaId)
                    .Include(i => i.Animal)
                    .Include(i => i.Finca)
                    .Include(i => i.RubroIngreso)
                    .OrderByDescending(i => i.IngresoId)
                    .ToListAsync();

                return View(ingresos);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado en Ingresoes.Index");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // GET: Ingresoes/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var fincaId = GetFincaId();

                var ingreso = await _context.Ingresos
                    .Where(i => i.FincaId == fincaId && i.IngresoId == id)
                    .Include(i => i.Animal)
                    .Include(i => i.Finca)
                    .Include(i => i.RubroIngreso)
                    .FirstOrDefaultAsync();

                if (ingreso == null)
                {
                    _logger.LogWarning($"Ingreso {id} no encontrado o no pertenece a finca {fincaId}");
                    return NotFound();
                }

                return View(ingreso);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // GET: Ingresoes/Create
        public IActionResult Create()
        {
            try
            {
                var fincaId = GetFincaId();

                // Solo animales activos (no vendidos ni muertos)
                ViewBag.AnimalId = new SelectList(
                    _context.Animals
                        .Where(a => a.FincaId == fincaId &&
                               a.Estado != "Vendido" &&
                               a.Estado != "Muerto")
                        .OrderBy(a => a.Arete),
                    "AnimalId", "Arete");

                ViewBag.RubroIngresoId = new SelectList(
                    _context.RubroIngresos, "RubroIngresoId", "Nombre");

                return View();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // POST: Ingresoes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IngresoId,RubroIngresoId,Fecha,Monto,Descripcion,AnimalId")] Ingreso ingreso)
        {
            try
            {
                var fincaId = GetFincaId();
                ingreso.FincaId = fincaId;

                // Los ingresos creados manualmente NO son automáticos
                ingreso.EsAutomatico = false;
                ingreso.OrigenModulo = FinanzasConstants.OrigenModulos.MANUAL;
                ingreso.ReferenciaOrigenId = null;

                ModelState.Remove("FincaId");
                ModelState.Remove("Finca");
                ModelState.Remove("Animal");
                ModelState.Remove("RubroIngreso");

                // Verificar que el animal esté disponible
                if (ingreso.AnimalId.HasValue)
                {
                    var animal = await _context.Animals
                        .FirstOrDefaultAsync(a => a.AnimalId == ingreso.AnimalId && a.FincaId == fincaId);

                    if (animal == null)
                    {
                        ModelState.AddModelError("AnimalId", "El animal seleccionado no pertenece a su finca.");
                    }
                    else if (animal.Estado == "Vendido")
                    {
                        ModelState.AddModelError("AnimalId", "No se puede registrar un ingreso para un animal que ya fue vendido.");
                    }
                    else if (animal.Estado == "Muerto")
                    {
                        ModelState.AddModelError("AnimalId", "No se puede registrar un ingreso para un animal que está muerto.");
                    }
                }

                if (ModelState.IsValid)
                {
                    _context.Ingresos.Add(ingreso);
                    await _context.SaveChangesAsync();
                    MostrarExito("Ingreso registrado exitosamente");
                    return RedirectToAction(nameof(Index));
                }

                // Recarga de dropdowns si falla - SOLO ANIMALES DISPONIBLES
                ViewBag.AnimalId = new SelectList(
                    _context.Animals
                        .Where(a => a.FincaId == fincaId &&
                               a.Estado != "Vendido" &&
                               a.Estado != "Muerto")
                        .OrderBy(a => a.Arete),
                    "AnimalId", "Arete", ingreso.AnimalId);

                ViewBag.RubroIngresoId = new SelectList(
                    _context.RubroIngresos, "RubroIngresoId", "Nombre", ingreso.RubroIngresoId);

                return View(ingreso);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // GET: Ingresoes/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var fincaId = GetFincaId();

                var ingreso = await _context.Ingresos
                    .Where(i => i.FincaId == fincaId && i.IngresoId == id)
                    .FirstOrDefaultAsync();

                if (ingreso == null)
                {
                    return NotFound();
                }

                // No permitir editar registros automáticos
                if (ingreso.EsAutomatico)
                {
                    MostrarError("No se pueden editar registros automáticos generados por el sistema. Use el módulo correspondiente para hacer cambios.");
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Solo animales activos (no vendidos ni muertos)
                ViewBag.AnimalId = new SelectList(
                    _context.Animals
                        .Where(a => a.FincaId == fincaId &&
                               a.Estado != "Vendido" &&
                               a.Estado != "Muerto")
                        .OrderBy(a => a.Arete),
                    "AnimalId", "Arete", ingreso.AnimalId);

                ViewBag.RubroIngresoId = new SelectList(
                    _context.RubroIngresos, "RubroIngresoId", "Nombre", ingreso.RubroIngresoId);

                return View(ingreso);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // POST: Ingresoes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id,
            [Bind("IngresoId,FincaId,RubroIngresoId,Fecha,Monto,Descripcion,AnimalId")] Ingreso ingreso)
        {
            if (id != ingreso.IngresoId)
                return NotFound();

            try
            {
                var fincaId = GetFincaId();

                //Obtener el ingreso original para verificar si es automático
                var ingresoOriginal = await _context.Ingresos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.IngresoId == id && i.FincaId == fincaId);

                if (ingresoOriginal == null)
                {
                    return NotFound();
                }

                // BLOQUEAR EDICIÓN DE REGISTROS AUTOMÁTICOS
                if (ingresoOriginal.EsAutomatico)
                {
                    MostrarError("No se pueden editar registros automáticos generados por el sistema.");
                    return RedirectToAction(nameof(Details), new { id });
                }

                ModelState.Remove("FincaId");
                ModelState.Remove("Finca");
                ModelState.Remove("Animal");
                ModelState.Remove("RubroIngreso");

                ValidarAcceso(ingreso.FincaId);

                // Verificar estado del animal
                if (ingreso.AnimalId.HasValue)
                {
                    var animal = await _context.Animals
                        .FirstOrDefaultAsync(a => a.AnimalId == ingreso.AnimalId && a.FincaId == fincaId);

                    if (animal == null)
                    {
                        ModelState.AddModelError("AnimalId", "El animal seleccionado no pertenece a su finca.");
                    }
                    else if (animal.Estado == "Vendido")
                    {
                        ModelState.AddModelError("AnimalId", "No se puede registrar un ingreso para un animal que ya fue vendido.");
                    }
                    else if (animal.Estado == "Muerto")
                    {
                        ModelState.AddModelError("AnimalId", "No se puede registrar un ingreso para un animal que está muerto.");
                    }
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        //PRESERVAR campos de automatización
                        ingreso.EsAutomatico = ingresoOriginal.EsAutomatico;
                        ingreso.OrigenModulo = ingresoOriginal.OrigenModulo;
                        ingreso.ReferenciaOrigenId = ingresoOriginal.ReferenciaOrigenId;

                        _context.Update(ingreso);
                        await _context.SaveChangesAsync();
                        MostrarExito("Ingreso actualizado exitosamente");
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!IngresoExists(id, fincaId))
                            return NotFound();
                        else
                            throw;
                    }

                    return RedirectToAction(nameof(Index));
                }

                // Recarga de dropdowns si falla - SOLO ANIMALES DISPONIBLES
                ViewBag.AnimalId = new SelectList(
                    _context.Animals
                        .Where(a => a.FincaId == fincaId &&
                               a.Estado != "Vendido" &&
                               a.Estado != "Muerto")
                        .OrderBy(a => a.Arete),
                    "AnimalId", "Arete", ingreso.AnimalId);

                ViewBag.RubroIngresoId = new SelectList(
                    _context.RubroIngresos, "RubroIngresoId", "Nombre", ingreso.RubroIngresoId);

                return View(ingreso);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // GET: Ingresoes/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var fincaId = GetFincaId();

                var ingreso = await _context.Ingresos
                    .Where(i => i.FincaId == fincaId && i.IngresoId == id)
                    .Include(i => i.Animal)
                    .Include(i => i.Finca)
                    .Include(i => i.RubroIngreso)
                    .FirstOrDefaultAsync();

                if (ingreso == null)
                    return NotFound();

                // ADVERTENCIA SI ES AUTOMÁTICO
                if (ingreso.EsAutomatico)
                {
                    ViewBag.EsAutomatico = true;
                    ViewBag.OrigenModulo = ingreso.OrigenModulo;
                }

                return View(ingreso);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // POST: Ingresoes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var fincaId = GetFincaId();

                var ingreso = await _context.Ingresos
                    .Where(i => i.FincaId == fincaId && i.IngresoId == id)
                    .FirstOrDefaultAsync();

                if (ingreso == null)
                    return NotFound();

                // NO PERMITIR ELIMINAR REGISTROS AUTOMÁTICOS
                if (ingreso.EsAutomatico)
                {
                    MostrarError($"No se pueden eliminar registros automáticos. Este ingreso fue generado por el módulo: {ingreso.OrigenModulo}. Use el módulo correspondiente para revertir la operación.");
                    return RedirectToAction(nameof(Index));
                }

                _context.Ingresos.Remove(ingreso);
                await _context.SaveChangesAsync();

                MostrarExito("Ingreso eliminado exitosamente");
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        private bool IngresoExists(long id, long fincaId)
        {
            return _context.Ingresos.Any(i => i.IngresoId == id && i.FincaId == fincaId);
        }
    }
}