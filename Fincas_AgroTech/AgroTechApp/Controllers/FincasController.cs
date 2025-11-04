using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AgroTechApp.Models.DB;

namespace AgroTechApp.Controllers
{
    public class FincasController : Controller
    {
        private readonly AgroTechDbContext _context;

        public FincasController(AgroTechDbContext context)
        {
            _context = context;
        }

        // GET: Fincas
        public async Task<IActionResult> Index()
        {
            return View(await _context.Fincas.ToListAsync());
        }

        // GET: Fincas/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var finca = await _context.Fincas
                .FirstOrDefaultAsync(m => m.FincaId == id);
            if (finca == null)
            {
                return NotFound();
            }

            return View(finca);
        }

        // GET: Fincas/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Fincas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FincaId,Nombre,Ubicacion,Activa,FechaCreacion")] Finca finca)
        {
            if (ModelState.IsValid)
            {
                _context.Add(finca);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(finca);
        }

        // GET: Fincas/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var finca = await _context.Fincas.FindAsync(id);
            if (finca == null)
            {
                return NotFound();
            }
            return View(finca);
        }

        // POST: Fincas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("FincaId,Nombre,Ubicacion,Activa,FechaCreacion")] Finca finca)
        {
            if (id != finca.FincaId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(finca);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FincaExists(finca.FincaId))
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
            return View(finca);
        }

        // GET: Fincas/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var finca = await _context.Fincas
                .FirstOrDefaultAsync(m => m.FincaId == id);
            if (finca == null)
            {
                return NotFound();
            }

            return View(finca);
        }

        // POST: Fincas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var finca = await _context.Fincas.FindAsync(id);
            if (finca != null)
            {
                _context.Fincas.Remove(finca);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FincaExists(long id)
        {
            return _context.Fincas.Any(e => e.FincaId == id);
        }
    }
}
