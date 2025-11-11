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
    public class IngresoesController : Controller
    {
        private readonly AgroTechDbContext _context;

        public IngresoesController(AgroTechDbContext context)
        {
            _context = context;
        }

        // GET: Ingresoes
        public async Task<IActionResult> Index()
        {
            var agroTechDbContext = _context.Ingresos.Include(i => i.Animal).Include(i => i.Finca).Include(i => i.RubroIngreso);
            return View(await agroTechDbContext.ToListAsync());
        }

        // GET: Ingresoes/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ingreso = await _context.Ingresos
                .Include(i => i.Animal)
                .Include(i => i.Finca)
                .Include(i => i.RubroIngreso)
                .FirstOrDefaultAsync(m => m.IngresoId == id);
            if (ingreso == null)
            {
                return NotFound();
            }

            return View(ingreso);
        }

        // GET: Ingresoes/Create
        public IActionResult Create()
        {
            ViewData["AnimalId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId");
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId");
            ViewData["RubroIngresoId"] = new SelectList(_context.RubroIngresos, "RubroIngresoId", "RubroIngresoId");
            return View();
        }

        // POST: Ingresoes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IngresoId,FincaId,RubroIngresoId,Fecha,Monto,Descripcion,AnimalId")] Ingreso ingreso)
        {
            if (ModelState.IsValid)
            {
                _context.Add(ingreso);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AnimalId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId", ingreso.AnimalId);
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId", ingreso.FincaId);
            ViewData["RubroIngresoId"] = new SelectList(_context.RubroIngresos, "RubroIngresoId", "RubroIngresoId", ingreso.RubroIngresoId);
            return View(ingreso);
        }

        // GET: Ingresoes/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ingreso = await _context.Ingresos.FindAsync(id);
            if (ingreso == null)
            {
                return NotFound();
            }
            ViewData["AnimalId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId", ingreso.AnimalId);
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId", ingreso.FincaId);
            ViewData["RubroIngresoId"] = new SelectList(_context.RubroIngresos, "RubroIngresoId", "RubroIngresoId", ingreso.RubroIngresoId);
            return View(ingreso);
        }

        // POST: Ingresoes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("IngresoId,FincaId,RubroIngresoId,Fecha,Monto,Descripcion,AnimalId")] Ingreso ingreso)
        {
            if (id != ingreso.IngresoId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ingreso);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IngresoExists(ingreso.IngresoId))
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
            ViewData["AnimalId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId", ingreso.AnimalId);
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId", ingreso.FincaId);
            ViewData["RubroIngresoId"] = new SelectList(_context.RubroIngresos, "RubroIngresoId", "RubroIngresoId", ingreso.RubroIngresoId);
            return View(ingreso);
        }

        // GET: Ingresoes/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ingreso = await _context.Ingresos
                .Include(i => i.Animal)
                .Include(i => i.Finca)
                .Include(i => i.RubroIngreso)
                .FirstOrDefaultAsync(m => m.IngresoId == id);
            if (ingreso == null)
            {
                return NotFound();
            }

            return View(ingreso);
        }

        // POST: Ingresoes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var ingreso = await _context.Ingresos.FindAsync(id);
            if (ingreso != null)
            {
                _context.Ingresos.Remove(ingreso);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool IngresoExists(long id)
        {
            return _context.Ingresos.Any(e => e.IngresoId == id);
        }
    }
}
