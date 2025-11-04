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
    public class AnimalesController : Controller
    {
        private readonly AgroTechDbContext _context;
        private readonly ILogger<AnimalesController> _logger;
        public AnimalesController(AgroTechDbContext context, ILogger<AnimalesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Animales
        public async Task<IActionResult> Index()
        {
            var agroTechDbContext = _context.Animals.Include(a => a.Finca).Include(a => a.LoteAnimal).Include(a => a.Madre).Include(a => a.Padre).Include(a => a.Raza);
            return View(await agroTechDbContext.ToListAsync());
        }

        // GET: Animales/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var animal = await _context.Animals
                .Include(a => a.Finca)
                .Include(a => a.LoteAnimal)
                .Include(a => a.Madre)
                .Include(a => a.Padre)
                .Include(a => a.Raza)
                .FirstOrDefaultAsync(m => m.AnimalId == id);
            if (animal == null)
            {
                return NotFound();
            }

            return View(animal);
        }

        // GET: Animales/Create
        public IActionResult Create()
        {
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "Nombre");
            ViewData["LoteAnimalId"] = new SelectList(_context.LoteAnimals, "LoteAnimalId", "Nombre");
            ViewData["MadreId"] = new SelectList(_context.Animals, "AnimalId", "Nombre");
            ViewData["PadreId"] = new SelectList(_context.Animals, "AnimalId", "Nombre");
            ViewData["RazaId"] = new SelectList(_context.Razas, "RazaId", "Nombre");
            return View();
        }

        // POST: Animales/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AnimalId,FincaId,Arete,Nombre,Sexo,RazaId,FechaNacimiento,PesoNacimiento,Estado,MadreId,PadreId,LoteAnimalId")] Animal animal)
        {

            ModelState.Remove("Finca");
            ModelState.Remove("LoteAnimal");
            ModelState.Remove("Madre");
            ModelState.Remove("Padre");
            ModelState.Remove("Raza");
            var fincaRaw = Request.HasFormContentType ? Request.Form["FincaId"].ToString() : "(no form)";
            _logger.LogInformation("[DEBUG] FincaId en form: '{FincaId}'", fincaRaw);
            _logger.LogInformation("[DEBUG] ModelState.IsValid antes de chequeo: {IsValid}", ModelState.IsValid);

            if (!ModelState.IsValid)
            {
                // Logueo de errores cuando SÍ hay errores
                foreach (var entry in ModelState)
                {
                    foreach (var error in entry.Value.Errors)
                    {
                        Console.WriteLine($"Campo: {entry.Key} → Error: {error.ErrorMessage}");
                    }
                }

                // Repoblar selects Y mantener selección actual
                ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "Nombre", animal.FincaId);
                ViewData["LoteAnimalId"] = new SelectList(_context.LoteAnimals, "LoteAnimalId", "Nombre", animal.LoteAnimalId);
                ViewData["MadreId"] = new SelectList(_context.Animals, "AnimalId", "Nombre", animal.MadreId);
                ViewData["PadreId"] = new SelectList(_context.Animals, "AnimalId", "Nombre", animal.PadreId);
                ViewData["RazaId"] = new SelectList(_context.Razas, "RazaId", "Nombre", animal.RazaId);

                return View(animal); // <- ¡regresa el modelo para no perder datos!
            }

            _context.Add(animal);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Animales/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var animal = await _context.Animals.FindAsync(id);
            if (animal == null)
            {
                return NotFound();
            }
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId", animal.FincaId);
            ViewData["LoteAnimalId"] = new SelectList(_context.LoteAnimals, "LoteAnimalId", "LoteAnimalId", animal.LoteAnimalId);
            ViewData["MadreId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId", animal.MadreId);
            ViewData["PadreId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId", animal.PadreId);
            ViewData["RazaId"] = new SelectList(_context.Razas, "RazaId", "RazaId", animal.RazaId);
            return View(animal);
        }

        // POST: Animales/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("AnimalId,FincaId,Arete,Nombre,Sexo,RazaId,FechaNacimiento,PesoNacimiento,Estado,MadreId,PadreId,LoteAnimalId")] Animal animal)
        {
            if (id != animal.AnimalId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(animal);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnimalExists(animal.AnimalId))
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
            ViewData["FincaId"] = new SelectList(_context.Fincas, "FincaId", "FincaId", animal.FincaId);
            ViewData["LoteAnimalId"] = new SelectList(_context.LoteAnimals, "LoteAnimalId", "LoteAnimalId", animal.LoteAnimalId);
            ViewData["MadreId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId", animal.MadreId);
            ViewData["PadreId"] = new SelectList(_context.Animals, "AnimalId", "AnimalId", animal.PadreId);
            ViewData["RazaId"] = new SelectList(_context.Razas, "RazaId", "RazaId", animal.RazaId);
            return View(animal);
        }

        // GET: Animales/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var animal = await _context.Animals
                .Include(a => a.Finca)
                .Include(a => a.LoteAnimal)
                .Include(a => a.Madre)
                .Include(a => a.Padre)
                .Include(a => a.Raza)
                .FirstOrDefaultAsync(m => m.AnimalId == id);
            if (animal == null)
            {
                return NotFound();
            }

            return View(animal);
        }

        // POST: Animales/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var animal = await _context.Animals.FindAsync(id);
            var fincaRaw = Request.HasFormContentType ? Request.Form["FincaId"].ToString() : "(no form)";
            Console.WriteLine($"[DEBUG] FincaId recibido: '{fincaRaw}'");
            if (animal != null)
            {
                _context.Animals.Remove(animal);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AnimalExists(long id)
        {
            return _context.Animals.Any(e => e.AnimalId == id);
        }
    }
}
