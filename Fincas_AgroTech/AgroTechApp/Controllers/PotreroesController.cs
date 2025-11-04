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
    public class PotreroesController : Controller
    {
        private readonly AgroTechDbContext _context;

        public PotreroesController(AgroTechDbContext context)
        {
            _context = context;
        }

        // GET: Potreroes
        public async Task<IActionResult> Index()
        {
            var agroTechDbContext = _context.Potreros.Include(p => p.Finca);
            return View(await agroTechDbContext.ToListAsync());
        }

        // GET: Potreroes/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var potrero = await _context.Potreros
                .Include(p => p.Finca)
                .FirstOrDefaultAsync(m => m.PotreroId == id);
            if (potrero == null)
            {
                return NotFound();
            }

            return View(potrero);
        }

        // GET: Potreroes/Create
        public IActionResult Create()
        {
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId");
            return View();
        }

        // POST: Potreroes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PotreroId,FincaId,Nombre,Hectareas,Activo")] Potrero potrero)
        {
            if (ModelState.IsValid)
            {
                _context.Add(potrero);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId", potrero.FincaId);
            return View(potrero);
        }

        // GET: Potreroes/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var potrero = await _context.Potreros.FindAsync(id);
            if (potrero == null)
            {
                return NotFound();
            }
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId", potrero.FincaId);
            return View(potrero);
        }

        // POST: Potreroes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("PotreroId,FincaId,Nombre,Hectareas,Activo")] Potrero potrero)
        {
            if (id != potrero.PotreroId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(potrero);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PotreroExists(potrero.PotreroId))
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
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId", potrero.FincaId);
            return View(potrero);
        }

        // GET: Potreroes/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var potrero = await _context.Potreros
                .Include(p => p.Finca)
                .FirstOrDefaultAsync(m => m.PotreroId == id);
            if (potrero == null)
            {
                return NotFound();
            }

            return View(potrero);
        }

        // POST: Potreroes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var potrero = await _context.Potreros.FindAsync(id);
            if (potrero != null)
            {
                _context.Potreros.Remove(potrero);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PotreroExists(long id)
        {
            return _context.Potreros.Any(e => e.PotreroId == id);
        }
    }
}
