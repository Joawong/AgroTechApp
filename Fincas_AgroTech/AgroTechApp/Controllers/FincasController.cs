using AgroTechApp.Models.DB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgroTechApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FincasController : BaseController
    {
        public FincasController(
            AgroTechDbContext context,
            ILogger<FincasController> logger)
            : base(context, logger)
        {
        }

        // =======================================================
        // GET: Fincas (lista solo las fincas del usuario)
        // =======================================================
        // GET: Fincas (lista solo las fincas del usuario con paginación)
        public async Task<IActionResult> Index(int? pagina, string? buscar, bool? soloActivas)
        {
            try
            {
                // Obtener TODAS las fincas del usuario (activas e inactivas)
                var fincasQuery = GetFincasUsuarioTodas().AsQueryable();

                // Filtro de búsqueda
                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    fincasQuery = fincasQuery.Where(f =>
                        f.Nombre.Contains(buscar) ||
                        (f.Ubicacion != null && f.Ubicacion.Contains(buscar)));
                }

                // Filtro de estado (solo activas)
                if (soloActivas.HasValue && soloActivas.Value)
                {
                    fincasQuery = fincasQuery.Where(f => f.Activa);
                }

                // Contar total antes de paginar
                var totalRegistros = fincasQuery.Count();

                // ORDEN: Más recientes primero (por FincaId descendente)
                var fincasOrdenadas = fincasQuery.OrderByDescending(f => f.FincaId).ToList();

                // PAGINACIÓN
                int registrosPorPagina = 10;
                int paginaActual = pagina ?? 1;
                int totalPaginas = (int)Math.Ceiling(totalRegistros / (double)registrosPorPagina);

                var fincasPaginadas = fincasOrdenadas
                    .Skip((paginaActual - 1) * registrosPorPagina)
                    .Take(registrosPorPagina)
                    .ToList();

                // ViewBags para la vista
                ViewBag.PaginaActual = paginaActual;
                ViewBag.TotalPaginas = totalPaginas;
                ViewBag.TotalRegistros = totalRegistros;
                ViewBag.BuscarTexto = buscar;
                ViewBag.SoloActivas = soloActivas ?? false;

                return View(fincasPaginadas);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando fincas");
                TempData["Error"] = "No se pudieron cargar las fincas.";
                return View(new List<Finca>());
            }
        }


        // =======================================================
        // GET: Details
        // =======================================================
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var fincasUsuario = GetFincasAsignadas();
                if (!fincasUsuario.Contains(id.Value))
                    return Unauthorized();

                var finca = await _context.Fincas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.FincaId == id);

                if (finca == null) return NotFound();

                return View(finca);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ver detalles de finca {Id}", id);
                TempData["Error"] = "No se pudieron cargar los detalles.";
                return RedirectToAction(nameof(Index));
            }
        }

        // =======================================================
        // GET: Create (solo administradores)
        // =======================================================

        public IActionResult Create()
        {
            try
            {
                return View(new Finca());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando vista de creación");
                TempData["Error"] = "No se pudo cargar el formulario.";
                return RedirectToAction(nameof(Index));
            }
        }

        // =======================================================
        // POST: Create
        // =======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Finca finca)
        {
            ModelState.Remove("Usuarios");
            ModelState.Remove("Animals");
            ModelState.Remove("Potreros");

            if (!ModelState.IsValid)
                return View(finca);

            try
            {
                // Validar nombre duplicado
                var existe = await _context.Fincas
                    .AnyAsync(f => f.Nombre.ToLower() == finca.Nombre.ToLower());

                if (existe)
                {
                    ModelState.AddModelError("Nombre", "Ya existe una finca con este nombre.");
                    return View(finca);
                }

                // Guardar la finca - IMPORTANTE: establecer como activa
                finca.Activa = true;
                finca.FechaCreacion = DateTime.Now;

                _context.Add(finca);
                await _context.SaveChangesAsync();

                // Asignarla al usuario actual
                var userId = GetAspNetUserId(); // ✅ CORRECCIÓN: Usar método de BaseController

                var uf = new UserFinca
                {
                    FincaId = finca.FincaId,
                    AspNetUserId = userId
                };

                _context.UserFincas.Add(uf);
                await _context.SaveChangesAsync();

                MostrarExito($"Finca '{finca.Nombre}' creada y asignada a usted.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear finca");
                TempData["Error"] = "No se pudo crear la finca.";
                return View(finca);
            }
        }



        // =======================================================
        // GET: Edit
        // =======================================================
        // GET: Edit
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var fincasUsuario = GetFincasAsignadas();
                if (!fincasUsuario.Contains(id.Value))
                    return Unauthorized();

                var finca = await _context.Fincas.FindAsync(id);

                if (finca == null) return NotFound();

                // Cargar estadísticas de la finca para mostrar en la vista
                ViewBag.TotalAnimales = await _context.Animals
                    .Where(a => a.FincaId == id && a.Estado == "Activo")
                    .CountAsync();

                ViewBag.TotalInsumos = await _context.Insumos
                    .Where(i => i.FincaId == id)
                    .CountAsync();

                return View(finca);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando edición para finca {Id}", id);
                TempData["Error"] = "No se pudo cargar el formulario.";
                return RedirectToAction(nameof(Index));
            }
        }

        // =======================================================
        // POST: Edit
        // =======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("FincaId,Nombre,Ubicacion,AreaTotal,Telefono,Email")] Finca finca)
        {
            if (id != finca.FincaId)
                return NotFound();

            var fincasUsuario = GetFincasAsignadas();
            if (!fincasUsuario.Contains(id))
                return Unauthorized();

            // Recuperar finca original para mantener datos importantes
            var fincaOriginal = await _context.Fincas
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FincaId == id);

            if (fincaOriginal == null)
                return NotFound();

            // Mantener valores que no deben cambiar
            finca.Activa = fincaOriginal.Activa;
            finca.FechaCreacion = fincaOriginal.FechaCreacion;

            // Trimear espacios
            finca.Nombre = finca.Nombre?.Trim();
            finca.Ubicacion = finca.Ubicacion?.Trim();

            ModelState.Remove("Usuarios");
            ModelState.Remove("Animals");
            ModelState.Remove("Potreros");

            if (!ModelState.IsValid)
            {
                // Recargar estadísticas
                ViewBag.TotalAnimales = await _context.Animals
                    .Where(a => a.FincaId == id && a.Estado == "Activo")
                    .CountAsync();
                ViewBag.TotalInsumos = await _context.Insumos
                    .Where(i => i.FincaId == id)
                    .CountAsync();

                return View(finca);
            }

            try
            {
                // Validar nombre duplicado
                var existe = await _context.Fincas
                    .AnyAsync(f => f.Nombre.ToLower() == finca.Nombre.ToLower() &&
                                   f.FincaId != finca.FincaId);

                if (existe)
                {
                    ModelState.AddModelError("Nombre", "Ya existe otra finca con ese nombre.");

                    // Recargar estadísticas
                    ViewBag.TotalAnimales = await _context.Animals
                        .Where(a => a.FincaId == id && a.Estado == "Activo")
                        .CountAsync();
                    ViewBag.TotalInsumos = await _context.Insumos
                        .Where(i => i.FincaId == id)
                        .CountAsync();

                    return View(finca);
                }

                _context.Update(finca);
                await _context.SaveChangesAsync();

                MostrarExito("Finca actualizada correctamente.");
                return RedirectToAction(nameof(Details), new { id = finca.FincaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editando finca {Id}", id);
                TempData["Error"] = "No se pudo actualizar la finca.";
                return View(finca);
            }
        }


        // =======================================================
        // GET: Delete
        // =======================================================
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var fincasUsuario = GetFincasAsignadas();
                if (!fincasUsuario.Contains(id.Value))
                    return Unauthorized();

                var finca = await _context.Fincas
                    .FirstOrDefaultAsync(f => f.FincaId == id);

                if (finca == null)
                    return NotFound();

                return View(finca);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando eliminación de finca {Id}", id);
                TempData["Error"] = "No se pudo cargar la vista de eliminación.";
                return RedirectToAction(nameof(Index));
            }
        }

        // =======================================================
        // POST: DeleteConfirmed
        // =======================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var fincasUsuario = GetFincasAsignadas();
            if (!fincasUsuario.Contains(id))
                return Unauthorized();

            try
            {
                var finca = await _context.Fincas
                    .FirstOrDefaultAsync(f => f.FincaId == id);

                if (finca == null)
                    return NotFound();

                // no elimina — solo desactiva
                finca.Activa = false;

                _context.Update(finca);
                await _context.SaveChangesAsync();

                MostrarExito("La finca ha sido desactivada correctamente.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desactivando finca {Id}", id);
                TempData["Error"] = "No se pudo desactivar la finca.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }


        [HttpPost]
        public IActionResult CambiarFinca(long id)
        {
            var fincasUsuario = GetFincasAsignadas();

            if (!fincasUsuario.Contains(id))
                return Unauthorized();

            HttpContext.Session.SetString("FincaActiva", id.ToString());

            MostrarExito("Finca seleccionada correctamente.");
            return Redirect(Request.Headers["Referer"].ToString());
        }

        // ========================================================================
        // API: Obtener fincas del usuario (para dropdown)
        // ========================================================================
        [HttpGet]
        public IActionResult ObtenerFincasUsuario()
        {
            try
            {
                var fincas = GetFincasUsuario(); 

                var resultado = fincas.Select(f => new
                {
                    fincaId = f.FincaId,
                    nombre = f.Nombre,
                    ubicacion = f.Ubicacion,
                    activa = f.Activa
                }).ToList();

                return Json(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo fincas del usuario");
                return Json(new List<object>());
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activar(long id)
        {
            // USAR TODAS LAS FINCAS (activas + inactivas)
            var fincasUsuarioTodas = GetFincasUsuarioTodas();
            if (!fincasUsuarioTodas.Any(f => f.FincaId == id))
                return Unauthorized();

            try
            {
                var finca = await _context.Fincas
                    .FirstOrDefaultAsync(f => f.FincaId == id);

                if (finca == null)
                    return NotFound();

                finca.Activa = true;
                _context.Update(finca);
                await _context.SaveChangesAsync();

                MostrarExito("La finca ha sido activada correctamente.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activando finca {Id}", id);
                TempData["Error"] = "No se pudo activar la finca.";
                return RedirectToAction(nameof(Index));
            }
        }





    }

}