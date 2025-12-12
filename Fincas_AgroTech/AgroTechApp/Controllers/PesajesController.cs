using AgroTechApp.Models.DB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace AgroTechApp.Controllers
{
    [Authorize]
    public class PesajesController : BaseController
    {
        public PesajesController(
            AgroTechDbContext context,
            ILogger<PesajesController> logger)
            : base(context, logger)
        {
        }

        // ============================
        // GET: Pesajes
        // ============================
        public async Task<IActionResult> Index()
        {
            try
            {
                var fincaId = GetFincaId();

                var pesajes = await _context.Pesajes
                    .Include(p => p.Animal)
                    .Where(p => p.Animal.FincaId == fincaId) // 🔒 MULTI-TENANT
                    .OrderByDescending(p => p.Fecha)
                    .ToListAsync();

                return View(pesajes);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================
        // GET: Pesajes/Create
        // ============================
        public IActionResult Create()
        {
            try
            {
                var fincaId = GetFincaId();

                var animales = _context.Animals
                    .Where(a => a.FincaId == fincaId) // 🔒 solo animales de la finca
                    .Select(a => new
                    {
                        a.AnimalId,
                        Texto = a.Arete + " - " + (a.Nombre ?? "(sin nombre)")
                    })
                    .ToList();

                ViewData["AnimalId"] = new SelectList(animales, "AnimalId", "Texto");

                return View(new Pesaje { Fecha = DateTime.Today });
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================
        // POST: Pesajes/Create
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PesajeId,AnimalId,Fecha,PesoKg,Observacion")] Pesaje pesaje)
        {
            try
            {
                var fincaId = GetFincaId();

                ModelState.Remove("Animal");

                // 🔒 Validar que el animal pertenezca a la finca del usuario
                var animal = await _context.Animals
                    .FirstOrDefaultAsync(a => a.AnimalId == pesaje.AnimalId && a.FincaId == fincaId);

                if (animal == null)
                {
                    ModelState.AddModelError("AnimalId", "El animal seleccionado no pertenece a su finca.");
                }

                if (!ModelState.IsValid)
                {
                    var animales = _context.Animals
                        .Where(a => a.FincaId == fincaId)
                        .Select(a => new
                        {
                            a.AnimalId,
                            Texto = a.Arete + " - " + (a.Nombre ?? "(sin nombre)")
                        }).ToList();

                    ViewData["AnimalId"] = new SelectList(animales, "AnimalId", "Texto", pesaje.AnimalId);
                    return View(pesaje);
                }

                _context.Add(pesaje);
                await _context.SaveChangesAsync();
                MostrarExito("Pesaje registrado correctamente.");
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================
        // GET: Pesajes/Details/5
        // ============================
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var fincaId = GetFincaId();

                var pesaje = await _context.Pesajes
                    .Include(p => p.Animal)
                    .FirstOrDefaultAsync(p => p.PesajeId == id && p.Animal.FincaId == fincaId); // 🔒 filtro

                if (pesaje == null) return NotFound();

                // Historial del mismo animal (solo el suyo)
                ViewBag.HistorialPesajes = await _context.Pesajes
                    .Where(p => p.AnimalId == pesaje.AnimalId)
                    .OrderBy(p => p.Fecha)
                    .AsNoTracking()
                    .ToListAsync();

                return View(pesaje);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================
        // GET: Pesajes/Edit/5
        // ============================
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var fincaId = GetFincaId();

                var pesaje = await _context.Pesajes
                    .Include(p => p.Animal)
                    .Where(p => p.PesajeId == id && p.Animal.FincaId == fincaId)
                    .FirstOrDefaultAsync();

                if (pesaje == null) return NotFound();

                var animales = await _context.Animals
                    .Where(a => a.FincaId == fincaId)
                    .Select(a => new
                    {
                        a.AnimalId,
                        Texto = a.Arete + " - " + (a.Nombre ?? "(sin nombre)")
                    })
                    .ToListAsync();

                ViewData["AnimalId"] = new SelectList(animales, "AnimalId", "Texto", pesaje.AnimalId);

                return View(pesaje);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================
        // POST: Pesajes/Edit/5
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("PesajeId,AnimalId,Fecha,PesoKg,Observacion")] Pesaje pesaje)
        {
            if (id != pesaje.PesajeId) return NotFound();

            try
            {
                var fincaId = GetFincaId();
                ModelState.Remove("Animal");

                // 🔒 Validar que el animal pertenece a la finca
                if (!await _context.Animals.AnyAsync(a => a.AnimalId == pesaje.AnimalId && a.FincaId == fincaId))
                {
                    ModelState.AddModelError("AnimalId", "Ese animal no pertenece a su finca.");
                }

                if (!ModelState.IsValid)
                {
                    var animales = await _context.Animals
                        .Where(a => a.FincaId == fincaId)
                        .Select(a => new
                        {
                            a.AnimalId,
                            Texto = a.Arete + " - " + (a.Nombre ?? "(sin nombre)")
                        }).ToListAsync();

                    ViewData["AnimalId"] = new SelectList(animales, "AnimalId", "Texto", pesaje.AnimalId);
                    return View(pesaje);
                }

                _context.Update(pesaje);
                await _context.SaveChangesAsync();
                MostrarExito("Pesaje actualizado correctamente.");

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================
        // GET: Pesajes/Delete/5
        // ============================
        public async Task<IActionResult> Delete(long? id)
        {
            if (id is null) return NotFound();

            try
            {
                var fincaId = GetFincaId();

                var pesaje = await _context.Pesajes
                    .Include(p => p.Animal)
                    .FirstOrDefaultAsync(p => p.PesajeId == id && p.Animal.FincaId == fincaId);

                if (pesaje == null) return NotFound();

                return View(pesaje);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================
        // POST: Pesajes/Delete/5
        // ============================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var fincaId = GetFincaId();

                var pesaje = await _context.Pesajes
                    .Include(p => p.Animal)
                    .Where(p => p.PesajeId == id && p.Animal.FincaId == fincaId)
                    .FirstOrDefaultAsync();

                if (pesaje == null) return NotFound();

                _context.Pesajes.Remove(pesaje);
                await _context.SaveChangesAsync();
                MostrarExito("Pesaje eliminado.");

                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }
    }
}
