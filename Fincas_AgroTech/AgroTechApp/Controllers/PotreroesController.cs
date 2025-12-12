using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AgroTechApp.Models.DB;

namespace AgroTechApp.Controllers
{
    [Authorize]
    public class PotreroesController : BaseController
    {
        public PotreroesController(
            AgroTechDbContext context,
            ILogger<PotreroesController> logger)
            : base(context, logger)
        {
        }

        // ===============================
        // GET: Potreros
        // ===============================
        public async Task<IActionResult> Index()
        {
            try
            {
                var fincaId = GetFincaId();

                var potreros = await _context.Potreros
                    .Where(p => p.FincaId == fincaId)      // 🔒 MULTI-TENANT
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();

                return View(potreros);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ===============================
        // GET: Details
        // ===============================
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var fincaId = GetFincaId();

                var potrero = await _context.Potreros
                    .Include(p => p.Finca)
                    .Include(p => p.Gastos)
                    .Include(p => p.MovimientoAnimals)
                        .ThenInclude(m => m.Animal)
                    .FirstOrDefaultAsync(p => p.PotreroId == id && p.FincaId == fincaId); // 🔒 filtro

                if (potrero == null) return NotFound();

                // Estadísticas internas
                ViewBag.AnimalesActuales = potrero.MovimientoAnimals.Count(m => m.FechaHasta == null);
                ViewBag.AnimalesTotales = potrero.MovimientoAnimals.Count;
                ViewBag.GastosTotales = potrero.Gastos.Sum(g => g.Monto);

                return View(potrero);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ===============================
        // GET: Create
        // ===============================
        public IActionResult Create()
        {
            try
            {
                var fincaId = GetFincaId();

                // NO permitir seleccionar finca → siempre la del usuario
                ViewBag.FincaNombre = _context.Fincas
                    .Where(f => f.FincaId == fincaId)
                    .Select(f => f.Nombre)
                    .FirstOrDefault();

                return View(new Potrero());
            }
            catch
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ===============================
        // POST: Create
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Hectareas,Activo")] Potrero potrero)
        {
            try
            {
                var fincaId = GetFincaId();
                potrero.FincaId = fincaId; // 🔒 asignar finca del usuario

                ModelState.Remove("Finca");
                ModelState.Remove("Gastos");
                ModelState.Remove("MovimientoAnimals");

                if (potrero.Hectareas <= 0)
                    ModelState.AddModelError("Hectareas", "Debe ser mayor a 0.");

                if (!ModelState.IsValid)
                    return View(potrero);

                // Validar nombre duplicado en la misma finca
                if (await _context.Potreros.AnyAsync(p =>
                        p.Nombre.ToLower() == potrero.Nombre.ToLower() &&
                        p.FincaId == fincaId))
                {
                    ModelState.AddModelError("Nombre", "Ya existe un potrero con ese nombre.");
                    return View(potrero);
                }

                _context.Potreros.Add(potrero);
                await _context.SaveChangesAsync();
                MostrarExito("Potrero creado exitosamente.");

                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ===============================
        // GET: Edit
        // ===============================
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var fincaId = GetFincaId();

                var potrero = await _context.Potreros
                    .FirstOrDefaultAsync(p => p.PotreroId == id && p.FincaId == fincaId);

                if (potrero == null) return NotFound();

                return View(potrero);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ===============================
        // POST: Edit
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("PotreroId,Nombre,Hectareas,Activo")] Potrero potrero)
        {
            try
            {
                var fincaId = GetFincaId();

                var original = await _context.Potreros
                    .FirstOrDefaultAsync(p => p.PotreroId == id && p.FincaId == fincaId);

                if (original == null)
                    return NotFound();

                if (potrero.Hectareas <= 0)
                    ModelState.AddModelError("Hectareas", "Debe ser mayor a 0.");

                if (!ModelState.IsValid)
                    return View(potrero);

                // Validar duplicado
                if (await _context.Potreros.AnyAsync(p =>
                        p.Nombre.ToLower() == potrero.Nombre.ToLower() &&
                        p.FincaId == fincaId &&
                        p.PotreroId != id))
                {
                    ModelState.AddModelError("Nombre", "Ya existe un potrero con ese nombre.");
                    return View(potrero);
                }

                // Actualizar solo campos editables
                original.Nombre = potrero.Nombre;
                original.Hectareas = potrero.Hectareas;
                original.Activo = potrero.Activo;

                await _context.SaveChangesAsync();
                MostrarExito("Potrero actualizado.");

                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ===============================
        // GET: Delete
        // ===============================
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var fincaId = GetFincaId();

                var potrero = await _context.Potreros
                    .Include(p => p.Gastos)
                    .Include(p => p.MovimientoAnimals)
                    .FirstOrDefaultAsync(p => p.PotreroId == id && p.FincaId == fincaId);

                if (potrero == null) return NotFound();

                ViewBag.TieneGastos = potrero.Gastos.Any();
                ViewBag.TieneMovimientos = potrero.MovimientoAnimals.Any();
                ViewBag.AnimalesActuales =
                    potrero.MovimientoAnimals.Count(m => m.FechaHasta == null);

                return View(potrero);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ===============================
        // POST: Delete Confirmed
        // ===============================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var fincaId = GetFincaId();

                var potrero = await _context.Potreros
                    .Include(p => p.Gastos)
                    .Include(p => p.MovimientoAnimals)
                    .FirstOrDefaultAsync(p => p.PotreroId == id && p.FincaId == fincaId);

                if (potrero == null) return NotFound();

                if (potrero.Gastos.Any())
                {
                    MostrarAdvertencia("No se puede eliminar: tiene gastos.");
                    return RedirectToAction(nameof(Delete), new { id });
                }

                if (potrero.MovimientoAnimals.Any(m => m.FechaHasta == null))
                {
                    MostrarAdvertencia("No se puede eliminar: hay animales en este potrero.");
                    return RedirectToAction(nameof(Delete), new { id });
                }

                _context.Potreros.Remove(potrero);
                await _context.SaveChangesAsync();
                MostrarExito("Potrero eliminado.");

                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ===============================
        // AJAX: Obtener potreros por finca
        // ===============================
        [HttpGet]
        public async Task<IActionResult> GetByFinca()
        {
            try
            {
                var fincaId = GetFincaId();

                var potreros = await _context.Potreros
                    .Where(p => p.FincaId == fincaId && p.Activo)
                    .Select(p => new
                    {
                        p.PotreroId,
                        p.Nombre,
                        p.Hectareas,
                        AnimalesActuales = p.MovimientoAnimals.Count(m => m.FechaHasta == null)
                    })
                    .ToListAsync();

                return Json(new { success = true, data = potreros });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        // ===============================
        // AJAX: Animales en potrero
        // ===============================
        [HttpGet]
        public async Task<IActionResult> AnimalesEnPotrero(long id)
        {
            try
            {
                var fincaId = GetFincaId();

                // Validar potrero
                if (!_context.Potreros.Any(p => p.PotreroId == id && p.FincaId == fincaId))
                    return Unauthorized();

                var animales = await _context.MovimientoAnimals
                    .Where(m => m.PotreroId == id && m.FechaHasta == null)
                    .Include(m => m.Animal)
                    .Select(m => new
                    {
                        m.AnimalId,
                        m.Animal.Arete,
                        m.Animal.Nombre,
                        m.Animal.Sexo,
                        RazaNombre = m.Animal.Raza != null ? m.Animal.Raza.Nombre : "Sin raza",
                        m.FechaDesde
                    })
                    .ToListAsync();

                return Json(new { success = true, data = animales });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        // ===============================
        // AJAX: Estadísticas por finca
        // ===============================
        [HttpGet]
        public async Task<IActionResult> Estadisticas()
        {
            try
            {
                var fincaId = GetFincaId();

                var estadisticas = new
                {
                    TotalPotreros = await _context.Potreros.CountAsync(p => p.FincaId == fincaId),
                    PotrerosActivos = await _context.Potreros.CountAsync(p => p.FincaId == fincaId && p.Activo),
                    HectareasTotales = await _context.Potreros.Where(p => p.FincaId == fincaId).SumAsync(p => p.Hectareas),
                    PromedioHectareas = await _context.Potreros.Where(p => p.FincaId == fincaId).AverageAsync(p => p.Hectareas)
                };

                return Json(new { success = true, data = estadisticas });
            }
            catch
            {
                return Json(new { success = false });
            }
        }
    }
}
