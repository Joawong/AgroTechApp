using AgroTechApp.Models.DB;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AgroTechApp.Controllers
{
    [Authorize]
    public class PesajesController : BaseController
    {
        public PesajesController(
            AgroTechDbContext context,
            ILogger<PesajesController> logger)
            : base(context, logger)
        {
        }

        // ============================
        // GET: Pesajes
        // ============================
        public async Task<IActionResult> Index(int? pagina)
        {
            try
            {
                var fincaId = GetFincaId();

                var query = _context.Pesajes
                    .Include(p => p.Animal)
                    .Where(p => p.Animal.FincaId == fincaId) // MULTI-TENANT
                    .AsQueryable();

                // Contar total antes de paginar
                var totalRegistros = await query.CountAsync();

                // Más recientes primero (por PesajeId descendente)
                query = query.OrderByDescending(p => p.PesajeId);

                // PAGINACIÓN
                int registrosPorPagina = 10;
                int paginaActual = pagina ?? 1;
                int totalPaginas = (int)Math.Ceiling(totalRegistros / (double)registrosPorPagina);

                var pesajes = await query
                    .Skip((paginaActual - 1) * registrosPorPagina)
                    .Take(registrosPorPagina)
                    .ToListAsync();

                // ViewBags para la vista
                ViewBag.PaginaActual = paginaActual;
                ViewBag.TotalPaginas = totalPaginas;
                ViewBag.TotalRegistros = totalRegistros;

                return View(pesajes);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================
        // GET: Pesajes/Create
        // ============================
        public async Task<IActionResult> Create()
        {
            try
            {
                var fincaId = GetFincaId();

                // SOLO ANIMALES ACTIVOS
                ViewBag.AnimalId = new SelectList(
                    await _context.Animals
                        .Where(a => a.FincaId == fincaId && a.Estado == "Activo")
                        .OrderBy(a => a.Arete)
                        .Select(a => new
                        {
                            a.AnimalId,
                            Display = $"{a.Arete} - {a.Nombre ?? "Sin nombre"}"
                        })
                        .ToListAsync(),
                    "AnimalId",
                    "Display");

                return View();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================
        // POST: Pesajes/Create
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PesajeId,AnimalId,Fecha,PesoKg,Observacion")] Pesaje pesaje)
        {
            try
            {
                var fincaId = GetFincaId();

                ModelState.Remove("Animal");

                // Validar que el animal pertenezca a la finca del usuario
                var animal = await _context.Animals
                    .FirstOrDefaultAsync(a => a.AnimalId == pesaje.AnimalId && a.FincaId == fincaId);

                if (animal == null)
                {
                    ModelState.AddModelError("AnimalId", "El animal seleccionado no pertenece a su finca.");
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.AnimalId = new SelectList(
                        await _context.Animals
                            .Where(a => a.FincaId == fincaId && a.Estado == "Activo") 
                            .OrderBy(a => a.Arete)
                            .Select(a => new
                            {
                                a.AnimalId,
                                Display = $"{a.Arete} - {a.Nombre ?? "Sin nombre"}"
                            })
                            .ToListAsync(),
                        "AnimalId",
                        "Display",
                        pesaje.AnimalId);

                    return View(pesaje);
                }

                _context.Add(pesaje);
                await _context.SaveChangesAsync();
                MostrarExito("Pesaje registrado correctamente.");
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================
        // GET: Pesajes/Details/5
        // ============================
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var fincaId = GetFincaId();

                var pesaje = await _context.Pesajes
                    .Include(p => p.Animal)
                    .FirstOrDefaultAsync(p => p.PesajeId == id && p.Animal.FincaId == fincaId); // filtro

                if (pesaje == null) return NotFound();

                // Historial del mismo animal (solo el suyo)
                ViewBag.HistorialPesajes = await _context.Pesajes
                    .Where(p => p.AnimalId == pesaje.AnimalId)
                    .OrderBy(p => p.Fecha)
                    .AsNoTracking()
                    .ToListAsync();

                return View(pesaje);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================
        // GET: Pesajes/Edit/5
        // ============================
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var fincaId = GetFincaId();

                var pesaje = await _context.Pesajes
                    .Include(p => p.Animal)
                    .Where(p => p.PesajeId == id && p.Animal.FincaId == fincaId)
                    .FirstOrDefaultAsync();

                if (pesaje == null) return NotFound();

                // SOLO ANIMALES ACTIVOS 
                ViewBag.AnimalId = new SelectList(
                    await _context.Animals
                        .Where(a => a.FincaId == fincaId && a.Estado == "Activo")
                        .OrderBy(a => a.Arete)
                        .Select(a => new
                        {
                            a.AnimalId,
                            Display = $"{a.Arete} - {a.Nombre ?? "Sin nombre"}"
                        })
                        .ToListAsync(),
                    "AnimalId",
                    "Display",
                    pesaje.AnimalId);

                return View(pesaje);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================
        // POST: Pesajes/Edit/5
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("PesajeId,AnimalId,Fecha,PesoKg,Observacion")] Pesaje pesaje)
        {
            if (id != pesaje.PesajeId) return NotFound();

            try
            {
                var fincaId = GetFincaId();
                ModelState.Remove("Animal");

                // 🔒 Validar que el animal pertenece a la finca
                if (!await _context.Animals.AnyAsync(a => a.AnimalId == pesaje.AnimalId && a.FincaId == fincaId))
                {
                    ModelState.AddModelError("AnimalId", "Ese animal no pertenece a su finca.");
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.AnimalId = new SelectList(
                        await _context.Animals
                            .Where(a => a.FincaId == fincaId && a.Estado == "Activo") 
                            .OrderBy(a => a.Arete)
                            .Select(a => new
                            {
                                a.AnimalId,
                                Display = $"{a.Arete} - {a.Nombre ?? "Sin nombre"}"
                            })
                            .ToListAsync(),
                        "AnimalId",
                        "Display",
                        pesaje.AnimalId);

                    return View(pesaje);
                }

                _context.Update(pesaje);
                await _context.SaveChangesAsync();
                MostrarExito("Pesaje actualizado correctamente.");

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================
        // GET: Pesajes/Delete/5
        // ============================
        public async Task<IActionResult> Delete(long? id)
        {
            if (id is null) return NotFound();

            try
            {
                var fincaId = GetFincaId();

                var pesaje = await _context.Pesajes
                    .Include(p => p.Animal)
                    .FirstOrDefaultAsync(p => p.PesajeId == id && p.Animal.FincaId == fincaId);

                if (pesaje == null) return NotFound();

                return View(pesaje);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================
        // POST: Pesajes/Delete/5
        // ============================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var fincaId = GetFincaId();

                var pesaje = await _context.Pesajes
                    .Include(p => p.Animal)
                    .Where(p => p.PesajeId == id && p.Animal.FincaId == fincaId)
                    .FirstOrDefaultAsync();

                if (pesaje == null) return NotFound();

                _context.Pesajes.Remove(pesaje);
                await _context.SaveChangesAsync();
                MostrarExito("Pesaje eliminado.");

                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }



        [HttpGet]
        public async Task<IActionResult> ObtenerUltimoPesaje(long animalId)
        {
            try
            {
                var fincaId = GetFincaId();

                // Validar animal pertenece a la finca
                var pertenece = await _context.Animals
                    .AnyAsync(a => a.AnimalId == animalId && a.FincaId == fincaId);

                if (!pertenece)
                    return Unauthorized();

                var ultimoPesaje = await _context.Pesajes
                    .Where(p => p.AnimalId == animalId)
                    .OrderByDescending(p => p.Fecha)
                    .Select(p => new
                    {
                        peso = p.PesoKg,
                        fecha = p.Fecha
                    })
                    .FirstOrDefaultAsync();

                if (ultimoPesaje == null)
                    return Json(null);

                return Json(ultimoPesaje);
            }
            catch
            {
                return StatusCode(500);
            }
        }

    }
}
