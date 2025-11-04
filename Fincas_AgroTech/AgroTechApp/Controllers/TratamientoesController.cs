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
    public class TratamientoesController : Controller
    {
        private readonly AgroTechDbContext _context;

        public TratamientoesController(AgroTechDbContext context)
        {
            _context = context;
        }

        // GET: Tratamientoes
        public async Task<IActionResult> Index()
        {
            var agroTechDbContext = _context.Tratamientos.Include(t => t.Animal).Include(t => t.Finca).Include(t => t.Insumo).Include(t => t.Lote).Include(t => t.LoteAnimal).Include(t => t.TipoTrat);
            return View(await agroTechDbContext.ToListAsync());
        }

        // GET: Tratamientoes/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tratamiento = await _context.Tratamientos
                .Include(t => t.Animal)
                .Include(t => t.Finca)
                .Include(t => t.Insumo)
                .Include(t => t.Lote)
                .Include(t => t.LoteAnimal)
                .Include(t => t.TipoTrat)
                .FirstOrDefaultAsync(m => m.TratamientoId == id);
            if (tratamiento == null)
            {
                return NotFound();
            }

            return View(tratamiento);
        }

        // GET: Tratamientoes/Create
        public IActionResult Create()
        {
            ViewData["AnimalId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId");
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId");
            ViewData["InsumoId"] = new SelectList(_context.Insumos, "InsumoId", "InsumoId");
            ViewData["LoteId"] = new SelectList(_context.InsumoLotes, "LoteId", "LoteId");
            ViewData["LoteAnimalId"] = new SelectList(_context.LoteAnimals, "LoteAnimalId", "LoteAnimalId");
            ViewData["TipoTratId"] = new SelectList(_context.TipoTratamientos, "TipoTratId", "TipoTratId");
            return View();
        }

        // POST: Tratamientoes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TratamientoId,FincaId,TipoTratId,AnimalId,LoteAnimalId,Fecha,InsumoId,LoteId,Dosis,Via,Responsable,Observacion")] Tratamiento tratamiento)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tratamiento);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AnimalId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId", tratamiento.AnimalId);
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId", tratamiento.FincaId);
            ViewData["InsumoId"] = new SelectList(_context.Insumos, "InsumoId", "InsumoId", tratamiento.InsumoId);
            ViewData["LoteId"] = new SelectList(_context.InsumoLotes, "LoteId", "LoteId", tratamiento.LoteId);
            ViewData["LoteAnimalId"] = new SelectList(_context.LoteAnimals, "LoteAnimalId", "LoteAnimalId", tratamiento.LoteAnimalId);
            ViewData["TipoTratId"] = new SelectList(_context.TipoTratamientos, "TipoTratId", "TipoTratId", tratamiento.TipoTratId);
            return View(tratamiento);
        }

        // GET: Tratamientoes/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tratamiento = await _context.Tratamientos.FindAsync(id);
            if (tratamiento == null)
            {
                return NotFound();
            }
            ViewData["AnimalId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId", tratamiento.AnimalId);
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId", tratamiento.FincaId);
            ViewData["InsumoId"] = new SelectList(_context.Insumos, "InsumoId", "InsumoId", tratamiento.InsumoId);
            ViewData["LoteId"] = new SelectList(_context.InsumoLotes, "LoteId", "LoteId", tratamiento.LoteId);
            ViewData["LoteAnimalId"] = new SelectList(_context.LoteAnimals, "LoteAnimalId", "LoteAnimalId", tratamiento.LoteAnimalId);
            ViewData["TipoTratId"] = new SelectList(_context.TipoTratamientos, "TipoTratId", "TipoTratId", tratamiento.TipoTratId);
            return View(tratamiento);
        }

        // POST: Tratamientoes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("TratamientoId,FincaId,TipoTratId,AnimalId,LoteAnimalId,Fecha,InsumoId,LoteId,Dosis,Via,Responsable,Observacion")] Tratamiento tratamiento)
        {
            if (id != tratamiento.TratamientoId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tratamiento);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TratamientoExists(tratamiento.TratamientoId))
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
            ViewData["AnimalId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId", tratamiento.AnimalId);
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId", tratamiento.FincaId);
            ViewData["InsumoId"] = new SelectList(_context.Insumos, "InsumoId", "InsumoId", tratamiento.InsumoId);
            ViewData["LoteId"] = new SelectList(_context.InsumoLotes, "LoteId", "LoteId", tratamiento.LoteId);
            ViewData["LoteAnimalId"] = new SelectList(_context.LoteAnimals, "LoteAnimalId", "LoteAnimalId", tratamiento.LoteAnimalId);
            ViewData["TipoTratId"] = new SelectList(_context.TipoTratamientos, "TipoTratId", "TipoTratId", tratamiento.TipoTratId);
            return View(tratamiento);
        }

        // GET: Tratamientoes/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tratamiento = await _context.Tratamientos
                .Include(t => t.Animal)
                .Include(t => t.Finca)
                .Include(t => t.Insumo)
                .Include(t => t.Lote)
                .Include(t => t.LoteAnimal)
                .Include(t => t.TipoTrat)
                .FirstOrDefaultAsync(m => m.TratamientoId == id);
            if (tratamiento == null)
            {
                return NotFound();
            }

            return View(tratamiento);
        }

        // POST: Tratamientoes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var tratamiento = await _context.Tratamientos.FindAsync(id);
            if (tratamiento != null)
            {
                _context.Tratamientos.Remove(tratamiento);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TratamientoExists(long id)
        {
            return _context.Tratamientos.Any(e => e.TratamientoId == id);
        }
    }
}
