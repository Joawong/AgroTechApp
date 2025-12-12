using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AgroTechApp.Models.DB;

namespace AgroTechApp.Controllers
{
    [Authorize]
    public class LoteAnimalesController : BaseController
    {
        public LoteAnimalesController(
            AgroTechDbContext context,
            ILogger<LoteAnimalesController> logger)
            : base(context, logger)
        {
        }

        // GET: LoteAnimales
        public async Task<IActionResult> Index()
        {
            try
            {
                var fincaId = GetFincaId();

                var lotes = await _context.LoteAnimals
                    .Where(l => l.FincaId == fincaId)   // MULTI-TENANT
                    .OrderBy(l => l.Nombre)
                    .ToListAsync();

                return View(lotes);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // GET: LoteAnimales/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var fincaId = GetFincaId();

                var lote = await _context.LoteAnimals
                    .FirstOrDefaultAsync(l =>
                        l.LoteAnimalId == id &&
                        l.FincaId == fincaId);        // MULTI-TENANT

                if (lote == null)
                    return NotFound();

                return View(lote);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // GET: LoteAnimales/Create
        public IActionResult Create()
        {
            try
            {
                var fincaId = GetFincaId();

                ViewData["FincaNombre"] = _context.Fincas
                    .Where(f => f.FincaId == fincaId)
                    .Select(f => f.Nombre)
                    .FirstOrDefault();

                return View();
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // POST: LoteAnimales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Descripcion")] LoteAnimal lote)
        {
            try
            {
                var fincaId = GetFincaId();

                lote.FincaId = fincaId; // 🔒 multi-tenant: siempre se asigna la finca del usuario

                if (ModelState.IsValid)
                {
                    _context.Add(lote);
                    await _context.SaveChangesAsync();
                    MostrarExito("Lote creado exitosamente.");
                    return RedirectToAction(nameof(Index));
                }

                return View(lote);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // GET: LoteAnimales/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var fincaId = GetFincaId();

                var lote = await _context.LoteAnimals
                    .FirstOrDefaultAsync(l => l.LoteAnimalId == id && l.FincaId == fincaId);

                if (lote == null)
                    return NotFound();

                return View(lote);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // POST: LoteAnimales/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id,
            [Bind("LoteAnimalId,Nombre,Descripcion,FincaId")] LoteAnimal lote)
        {
            try
            {
                var fincaId = GetFincaId();

                // Validar acceso multi-tenant
                if (lote.FincaId != fincaId)
                    return Unauthorized();

                if (id != lote.LoteAnimalId)
                    return NotFound();

                if (ModelState.IsValid)
                {
                    try
                    {
                        // NO permitir cambiar FincaId
                        lote.FincaId = fincaId;

                        _context.Update(lote);
                        await _context.SaveChangesAsync();
                        MostrarExito("Lote actualizado.");
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!LoteExists(id, fincaId))
                            return NotFound();
                        else
                            throw;
                    }
                }

                return View(lote);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // GET: LoteAnimales/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var fincaId = GetFincaId();

                var lote = await _context.LoteAnimals
                    .FirstOrDefaultAsync(l => l.LoteAnimalId == id && l.FincaId == fincaId);

                if (lote == null)
                    return NotFound();

                return View(lote);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // POST: LoteAnimales/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var fincaId = GetFincaId();

                var lote = await _context.LoteAnimals
                    .FirstOrDefaultAsync(l => l.LoteAnimalId == id && l.FincaId == fincaId);

                if (lote == null)
                    return NotFound();

                _context.LoteAnimals.Remove(lote);
                await _context.SaveChangesAsync();

                MostrarExito("Lote eliminado.");
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        private bool LoteExists(long id, long fincaId) =>
            _context.LoteAnimals.Any(l => l.LoteAnimalId == id && l.FincaId == fincaId);
    }
}
