using AgroTechApp.Models.DB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AgroTechApp.Controllers
{
    public class PesajesController : Controller
    {
        private readonly AgroTechDbContext _context;
        private readonly ILogger<PesajesController> _logger;

        public PesajesController(AgroTechDbContext context, ILogger<PesajesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Pesajes
        public async Task<IActionResult> Index()
        {
            var agroTechDbContext = _context.Pesajes.Include(p => p.Animal);
            return View(await agroTechDbContext.ToListAsync());
        }

        // GET: Pesajes/Create
        public IActionResult Create()
        {
            // Texto amigable para el combo: Arete - Nombre
            var animales = _context.Animals
                .Select(a => new { a.AnimalId, Texto = a.Arete + " - " + (a.Nombre ?? "(sin nombre)") })
                .ToList();

            ViewData["AnimalId"] = new SelectList(animales, "AnimalId", "Texto");

            // Sugerencia: fecha hoy
            var modelo = new Pesaje { Fecha = DateTime.Today };
            return View(modelo);
        }

        // POST: Pesajes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PesajeId,AnimalId,Fecha,PesoKg,Observacion")] Pesaje pesaje)
        {
            // Evitar que el binder "toque" la navegación
            ModelState.Remove("Animal");

            // Logs que sí se ven
            var animalRaw = Request.HasFormContentType ? Request.Form["AnimalId"].ToString() : "(no form)";
            var fechaRaw = Request.HasFormContentType ? Request.Form["Fecha"].ToString() : "(no form)";
            var pesoRaw = Request.HasFormContentType ? Request.Form["PesoKg"].ToString() : "(no form)";
            _logger.LogInformation("[DEBUG] Form: AnimalId='{AnimalId}', Fecha='{Fecha}', PesoKg='{PesoKg}'", animalRaw, fechaRaw, pesoRaw);

            if (!ModelState.IsValid)
            {
                // Dump de errores
                foreach (var kv in ModelState)
                    foreach (var err in kv.Value.Errors)
                        _logger.LogWarning("[VALIDATION] {Key}: {Error}", kv.Key, err.ErrorMessage);

                var animales = _context.Animals
                    .Select(a => new { a.AnimalId, Texto = a.Arete + " - " + (a.Nombre ?? "(sin nombre)") })
                    .ToList();

                ViewData["AnimalId"] = new SelectList(animales, "AnimalId", "Texto", pesaje.AnimalId);
                return View(pesaje);
            }

            _context.Add(pesaje);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }



        // GET: Pesajes/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            var pesaje = await _context.Pesajes
                .Include(p => p.Animal) // <- importante para ver Arete/Nombre
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.PesajeId == id);

            if (pesaje == null) return NotFound();

            return View(pesaje);
        }

        // GET: Pesajes/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var pesaje = await _context.Pesajes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PesajeId == id);

            if (pesaje == null) return NotFound();

            var animales = _context.Animals
                .Select(a => new { a.AnimalId, Texto = a.Arete + " - " + (a.Nombre ?? "(sin nombre)") })
                .ToList();

            ViewData["AnimalId"] = new SelectList(animales, "AnimalId", "Texto", pesaje.AnimalId);
            return View(pesaje);
        }

        // POST: Pesajes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("PesajeId,AnimalId,Fecha,PesoKg,Observacion")] Pesaje pesaje)
        {
            if (id != pesaje.PesajeId) return NotFound();

            // Evitar que el binder se meta con la navegación
            ModelState.Remove("Animal");

            if (!ModelState.IsValid)
            {
                var animales = _context.Animals
                    .Select(a => new { a.AnimalId, Texto = a.Arete + " - " + (a.Nombre ?? "(sin nombre)") })
                    .ToList();
                ViewData["AnimalId"] = new SelectList(animales, "AnimalId", "Texto", pesaje.AnimalId);
                return View(pesaje);
            }

            try
            {
                _context.Update(pesaje);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Pesajes.Any(e => e.PesajeId == id)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }


        // GET: Pesajes/Delete/5
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

        // POST: Pesajes/Delete/5
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
