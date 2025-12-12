//using AgroTechApp.Models.DB;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;

//namespace AgroTechApp.Controllers
//{
//    public class MovimientoAnimalController : BaseController
//    {
//        public MovimientoAnimalController(
//            AgroTechDbContext context,
//            ILogger<MovimientoAnimalController> logger)
//            : base(context, logger)
//        {
//        }

//        // ===========================================================
//        // LISTADO GENERAL (solo animales de la finca del usuario)
//        // ===========================================================
//        public async Task<IActionResult> Index()
//        {
//            var fincaId = GetFincaId();

//            var lista = await _context.MovimientoAnimals
//                .Include(m => m.Animal)
//                .Include(m => m.Potrero)
//                .Where(m => m.FincaId == fincaId)
//                .OrderByDescending(m => m.FechaDesde)
//                .AsNoTracking()
//                .ToListAsync();

//            return View(lista);
//        }


//        // ===========================================================
//        // HISTORIAL por animal
//        // ===========================================================
//        public async Task<IActionResult> Historial(long animalId)
//        {
//            var fincaId = GetFincaId();

//            var animal = await _context.Animals
//                .FirstOrDefaultAsync(a => a.AnimalId == animalId && a.FincaId == fincaId);

//            if (animal is null)
//                return Unauthorized();

//            var hist = await _context.MovimientoAnimals
//                .Include(m => m.Potrero)
//                .Where(m => m.AnimalId == animalId)
//                .OrderByDescending(m => m.FechaDesde)
//                .AsNoTracking()
//                .ToListAsync();

//            ViewBag.Animal = animal;
//            return View("Historial", hist);
//        }


//        // ===========================================================
//        // GET: Registrar entrada
//        // ===========================================================
//        public async Task<IActionResult> Entrada()
//        {
//            var fincaId = GetFincaId();

//            await CargarCombos(fincaId);
//            return View(new MovimientoAnimal { FechaDesde = DateTime.Today });
//        }


//        // ===========================================================
//        // POST: Registrar entrada
//        // ===========================================================
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Entrada([Bind("AnimalId,PotreroId,FechaDesde,Observacion")] MovimientoAnimal mov)
//        {
//            var fincaId = GetFincaId();
//            mov.FincaId = fincaId;

//            // Validar animal
//            var animal = await _context.Animals
//                .FirstOrDefaultAsync(a => a.AnimalId == mov.AnimalId && a.FincaId == fincaId);

//            if (animal is null)
//                ModelState.AddModelError("AnimalId", "Animal no válido para esta finca.");

//            // Validar potrero
//            var potrero = await _context.Potreros
//                .FirstOrDefaultAsync(p => p.PotreroId == mov.PotreroId && p.FincaId == fincaId);

//            if (potrero is null)
//                ModelState.AddModelError("PotreroId", "Potrero no válido para esta finca.");

//            // Verificar si ya está en un potrero
//            var movActivo = await _context.MovimientoAnimals
//                .FirstOrDefaultAsync(m => m.AnimalId == mov.AnimalId && m.FechaHasta == null);

//            if (movActivo != null)
//            {
//                ModelState.AddModelError("", $"Este animal ya está en el potrero '{movActivo.PotreroId}' desde {movActivo.FechaDesde:dd/MM/yyyy}.");
//            }

//            if (!ModelState.IsValid)
//            {
//                await CargarCombos(fincaId, mov.AnimalId, mov.PotreroId);
//                return View(mov);
//            }

//            // Crear movimiento
//            _context.MovimientoAnimals.Add(mov);
//            await _context.SaveChangesAsync();

//            MostrarExito("Entrada registrada correctamente.");
//            return RedirectToAction(nameof(Index));
//        }


//        // ===========================================================
//        // GET: Registrar salida (cerrar movimiento activo)
//        // ===========================================================
//        public async Task<IActionResult> Salida(long id) // id = AnimalId
//        {
//            var fincaId = GetFincaId();

//            var mov = await _context.MovimientoAnimals
//                .Include(m => m.Animal)
//                .Include(m => m.Potrero)
//                .Where(m => m.AnimalId == id && m.FechaHasta == null && m.FincaId == fincaId)
//                .FirstOrDefaultAsync();

//            if (mov is null)
//            {
//                TempData["Error"] = "Este animal no está actualmente en ningún potrero.";
//                return RedirectToAction(nameof(Index));
//            }

//            mov.FechaHasta = DateTime.Today;
//            return View(mov);
//        }

//        // ===========================================================
//        // POST: Registrar salida
//        // ===========================================================
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Salida(long id, DateTime fechaHasta, string? observacion)
//        {
//            var fincaId = GetFincaId();

//            var mov = await _context.MovimientoAnimals
//                .Where(m => m.AnimalId == id && m.FechaHasta == null && m.FincaId == fincaId)
//                .FirstOrDefaultAsync();

//            if (mov is null)
//            {
//                TempData["Error"] = "No se encontró movimiento activo.";
//                return RedirectToAction(nameof(Index));
//            }

//            // Validaciones
//            if (fechaHasta < mov.FechaDesde)
//            {
//                TempData["Error"] = "La fecha de salida no puede ser anterior a la fecha de entrada.";
//                return RedirectToAction(nameof(Salida), new { id });
//            }

//            mov.FechaHasta = fechaHasta;
//            mov.Observacion = observacion;

//            await _context.SaveChangesAsync();
//            MostrarExito("Salida registrada correctamente.");

//            return RedirectToAction(nameof(Index));
//        }


//        // ===========================================================
//        // TRANSFERENCIA: salida + entrada automática
//        // ===========================================================
//        public async Task<IActionResult> Transferir(long id) // id = AnimalId
//        {
//            var fincaId = GetFincaId();

//            var movActivo = await _context.MovimientoAnimals
//                .Include(m => m.Potrero)
//                .FirstOrDefaultAsync(m => m.AnimalId == id && m.FechaHasta == null && m.FincaId == fincaId);

//            if (movActivo == null)
//            {
//                TempData["Error"] = "Este animal debe estar en un potrero para transferirlo.";
//                return RedirectToAction(nameof(Index));
//            }

//            await CargarPotreros(fincaId, excludeId: movActivo.PotreroId);

//            ViewBag.AnimalId = id;
//            ViewBag.AnimalTexto = (await _context.Animals.FindAsync(id))?.Arete;

//            return View();
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Transferir(long animalId, long nuevoPotreroId, DateTime fecha)
//        {
//            var fincaId = GetFincaId();

//            var movActivo = await _context.MovimientoAnimals
//                .Where(m => m.AnimalId == animalId && m.FechaHasta == null && m.FincaId == fincaId)
//                .FirstOrDefaultAsync();

//            if (movActivo == null)
//            {
//                TempData["Error"] = "No se encontró movimiento activo.";
//                return RedirectToAction(nameof(Index));
//            }

//            if (movActivo.FechaDesde > fecha)
//            {
//                TempData["Error"] = "La fecha de transferencia no puede ser anterior a la fecha de entrada.";
//                return RedirectToAction(nameof(Transferir), new { id = animalId });
//            }

//            using var tx = await _context.Database.BeginTransactionAsync();

//            // 1) Cerrar movimiento activo
//            movActivo.FechaHasta = fecha;

//            // 2) Crear nuevo movimiento
//            var nuevo = new MovimientoAnimal
//            {
//                AnimalId = animalId,
//                PotreroId = nuevoPotreroId,
//                FechaDesde = fecha,
//                FincaId = fincaId
//            };

//            _context.MovimientoAnimals.Add(nuevo);

//            await _context.SaveChangesAsync();
//            await tx.CommitAsync();

//            MostrarExito("Transferencia registrada correctamente.");
//            return RedirectToAction(nameof(Index));
//        }


//        // ===========================================================
//        // HELPERS
//        // ===========================================================
//        private async Task CargarCombos(long fincaId, long? animalSel = null, long? potreroSel = null)
//        {
//            var animales = await _context.Animals
//                .Where(a => a.FincaId == fincaId && !a.Fallecido)
//                .OrderBy(a => a.Arete)
//                .Select(a => new { a.AnimalId, Texto = a.Arete + " - " + (a.Nombre ?? "") })
//                .ToListAsync();

//            var potreros = await _context.Potreros
//                .Where(p => p.FincaId == fincaId && p.Activo)
//                .OrderBy(p => p.Nombre)
//                .Select(p => new { p.PotreroId, p.Nombre })
//                .ToListAsync();

//            ViewData["AnimalId"] = new SelectList(animales, "AnimalId", "Texto", animalSel);
//            ViewData["PotreroId"] = new SelectList(potreros, "PotreroId", "Nombre", potreroSel);
//        }

//        private async Task CargarPotreros(long fincaId, long? excludeId = null)
//        {
//            var potreros = await _context.Potreros
//                .Where(p => p.FincaId == fincaId && p.Activo && p.PotreroId != excludeId)
//                .Select(p => new { p.PotreroId, p.Nombre })
//                .OrderBy(p => p.Nombre)
//                .ToListAsync();

//            ViewData["PotreroId"] = new SelectList(potreros, "PotreroId", "Nombre");
//        }
//    }
//}
