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
    public class InsumoesController : Controller
    {
        private readonly AgroTechDbContext _context;

        public InsumoesController(AgroTechDbContext context)
        {
            _context = context;
        }

        // GET: Insumoes
        public async Task<IActionResult> Index()
        {
            var agroTechDbContext = _context.Insumos.Include(i => i.Categoria).Include(i => i.Finca).Include(i => i.Unidad);
            return View(await agroTechDbContext.ToListAsync());
        }

        // GET: Insumoes/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var insumo = await _context.Insumos
                .Include(i => i.Categoria)
                .Include(i => i.Finca)
                .Include(i => i.Unidad)
                .FirstOrDefaultAsync(m => m.InsumoId == id);
            if (insumo == null)
            {
                return NotFound();
            }

            return View(insumo);
        }

        // GET: Insumoes/Create
        public IActionResult Create()
        {
            ViewData["CategoriaId"] = new SelectList(_context.CategoriaInsumos, "CategoriaId", "CategoriaId");
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId");
            ViewData["UnidadId"] = new SelectList(_context.UnidadMedida, "UnidadId", "UnidadId");
            return View();
        }

        // POST: Insumoes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("InsumoId,FincaId,CategoriaId,Nombre,UnidadId,StockMinimo,Activo")] Insumo insumo)
        {
            if (ModelState.IsValid)
            {
                _context.Add(insumo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoriaId"] = new SelectList(_context.CategoriaInsumos, "CategoriaId", "CategoriaId", insumo.CategoriaId);
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId", insumo.FincaId);
            ViewData["UnidadId"] = new SelectList(_context.UnidadMedida, "UnidadId", "UnidadId", insumo.UnidadId);
            return View(insumo);
        }

        // GET: Insumoes/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var insumo = await _context.Insumos.FindAsync(id);
            if (insumo == null)
            {
                return NotFound();
            }
            ViewData["CategoriaId"] = new SelectList(_context.CategoriaInsumos, "CategoriaId", "CategoriaId", insumo.CategoriaId);
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId", insumo.FincaId);
            ViewData["UnidadId"] = new SelectList(_context.UnidadMedida, "UnidadId", "UnidadId", insumo.UnidadId);
            return View(insumo);
        }

        // POST: Insumoes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("InsumoId,FincaId,CategoriaId,Nombre,UnidadId,StockMinimo,Activo")] Insumo insumo)
        {
            if (id != insumo.InsumoId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(insumo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InsumoExists(insumo.InsumoId))
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
            ViewData["CategoriaId"] = new SelectList(_context.CategoriaInsumos, "CategoriaId", "CategoriaId", insumo.CategoriaId);
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId", insumo.FincaId);
            ViewData["UnidadId"] = new SelectList(_context.UnidadMedida, "UnidadId", "UnidadId", insumo.UnidadId);
            return View(insumo);
        }

        // GET: Insumoes/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var insumo = await _context.Insumos
                .Include(i => i.Categoria)
                .Include(i => i.Finca)
                .Include(i => i.Unidad)
                .FirstOrDefaultAsync(m => m.InsumoId == id);
            if (insumo == null)
            {
                return NotFound();
            }

            return View(insumo);
        }

        // POST: Insumoes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var insumo = await _context.Insumos.FindAsync(id);
            if (insumo != null)
            {
                _context.Insumos.Remove(insumo);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool InsumoExists(long id)
        {
            return _context.Insumos.Any(e => e.InsumoId == id);
        }
    }
}
