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
    public class GastoesController : Controller
    {
        private readonly AgroTechDbContext _context;

        public GastoesController(AgroTechDbContext context)
        {
            _context = context;
        }

        // GET: Gastoes
        public async Task<IActionResult> Index()
        {
            var agroTechDbContext = _context.Gastos.Include(g => g.Animal).Include(g => g.Finca).Include(g => g.Insumo).Include(g => g.Potrero).Include(g => g.RubroGasto);
            return View(await agroTechDbContext.ToListAsync());
        }

        // GET: Gastoes/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gasto = await _context.Gastos
                .Include(g => g.Animal)
                .Include(g => g.Finca)
                .Include(g => g.Insumo)
                .Include(g => g.Potrero)
                .Include(g => g.RubroGasto)
                .FirstOrDefaultAsync(m => m.GastoId == id);
            if (gasto == null)
            {
                return NotFound();
            }

            return View(gasto);
        }

        // GET: Gastoes/Create
        public IActionResult Create()
        {
            ViewData["AnimalId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId");
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId");
            ViewData["InsumoId"] = new SelectList(_context.Insumos, "InsumoId", "InsumoId");
            ViewData["PotreroId"] = new SelectList(_context.Potreros, "PotreroId", "PotreroId");
            ViewData["RubroGastoId"] = new SelectList(_context.RubroGastos, "RubroGastoId", "RubroGastoId");
            return View();
        }

        // POST: Gastoes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GastoId,FincaId,RubroGastoId,Fecha,Monto,Descripcion,AnimalId,PotreroId,InsumoId")] Gasto gasto)
        {
            if (ModelState.IsValid)
            {
                _context.Add(gasto);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AnimalId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId", gasto.AnimalId);
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId", gasto.FincaId);
            ViewData["InsumoId"] = new SelectList(_context.Insumos, "InsumoId", "InsumoId", gasto.InsumoId);
            ViewData["PotreroId"] = new SelectList(_context.Potreros, "PotreroId", "PotreroId", gasto.PotreroId);
            ViewData["RubroGastoId"] = new SelectList(_context.RubroGastos, "RubroGastoId", "RubroGastoId", gasto.RubroGastoId);
            return View(gasto);
        }

        // GET: Gastoes/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gasto = await _context.Gastos.FindAsync(id);
            if (gasto == null)
            {
                return NotFound();
            }
            ViewData["AnimalId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId", gasto.AnimalId);
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId", gasto.FincaId);
            ViewData["InsumoId"] = new SelectList(_context.Insumos, "InsumoId", "InsumoId", gasto.InsumoId);
            ViewData["PotreroId"] = new SelectList(_context.Potreros, "PotreroId", "PotreroId", gasto.PotreroId);
            ViewData["RubroGastoId"] = new SelectList(_context.RubroGastos, "RubroGastoId", "RubroGastoId", gasto.RubroGastoId);
            return View(gasto);
        }

        // POST: Gastoes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("GastoId,FincaId,RubroGastoId,Fecha,Monto,Descripcion,AnimalId,PotreroId,InsumoId")] Gasto gasto)
        {
            if (id != gasto.GastoId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(gasto);
                    await _context.SaveChangesAsync();
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
            ViewData["AnimalId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId", gasto.AnimalId);
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId", gasto.FincaId);
            ViewData["InsumoId"] = new SelectList(_context.Insumos, "InsumoId", "InsumoId", gasto.InsumoId);
            ViewData["PotreroId"] = new SelectList(_context.Potreros, "PotreroId", "PotreroId", gasto.PotreroId);
            ViewData["RubroGastoId"] = new SelectList(_context.RubroGastos, "RubroGastoId", "RubroGastoId", gasto.RubroGastoId);
            return View(gasto);
        }

        // GET: Gastoes/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gasto = await _context.Gastos
                .Include(g => g.Animal)
                .Include(g => g.Finca)
                .Include(g => g.Insumo)
                .Include(g => g.Potrero)
                .Include(g => g.RubroGasto)
                .FirstOrDefaultAsync(m => m.GastoId == id);
            if (gasto == null)
            {
                return NotFound();
            }

            return View(gasto);
        }

        // POST: Gastoes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var gasto = await _context.Gastos.FindAsync(id);
            if (gasto != null)
            {
                _context.Gastos.Remove(gasto);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GastoExists(long id)
        {
            return _context.Gastos.Any(e => e.GastoId == id);
        }
    }
}
