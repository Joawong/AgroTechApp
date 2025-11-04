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
    public class PesajesController : Controller
    {
        private readonly AgroTechDbContext _context;

        public PesajesController(AgroTechDbContext context)
        {
            _context = context;
        }

        // GET: Pesajes
        public async Task<IActionResult> Index()
        {
            var agroTechDbContext = _context.Pesajes.Include(p => p.Animal);
            return View(await agroTechDbContext.ToListAsync());
        }

        // GET: Pesajes/Details
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pesaje = await _context.Pesajes
                .Include(p => p.Animal)
                .FirstOrDefaultAsync(m => m.PesajeId == id);
            if (pesaje == null)
            {
                return NotFound();
            }

            return View(pesaje);
        }

        // GET: Pesajes/Create
        public IActionResult Create()
        {
            ViewData["AnimalId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId");
            return View();
        }

        // POST: Pesajes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PesajeId,AnimalId,Fecha,PesoKg,Observacion")] Pesaje pesaje)
        {
            if (ModelState.IsValid)
            {
                _context.Add(pesaje);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AnimalId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId", pesaje.AnimalId);
            return View(pesaje);
        }

        // GET: Pesajes/Edit
        public async Task<IActionResult> Edit
            (long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pesaje = await _context.Pesajes.FindAsync(id);
            if (pesaje == null)
            {
                return NotFound();
            }
            ViewData["AnimalId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId", pesaje.AnimalId);
            return View(pesaje);
        }

        // POST: Pesajes/Edit
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit
            (long id, [Bind("PesajeId,AnimalId,Fecha,PesoKg,Observacion")] Pesaje pesaje)
        {
            if (id != pesaje.PesajeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pesaje);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PesajeExists(pesaje.PesajeId))
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
            ViewData["AnimalId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId", pesaje.AnimalId);
            return View(pesaje);
        }

        // GET: Pesajes/Delete
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pesaje = await _context.Pesajes
                .Include(p => p.Animal)
                .FirstOrDefaultAsync(m => m.PesajeId == id);
            if (pesaje == null)
            {
                return NotFound();
            }

            return View(pesaje);
        }

        // POST: Pesajes/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var pesaje = await _context.Pesajes.FindAsync(id);
            if (pesaje != null)
            {
                _context.Pesajes.Remove(pesaje);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PesajeExists(long id)
        {
            return _context.Pesajes.Any(e => e.PesajeId == id);
        }
    }
}
