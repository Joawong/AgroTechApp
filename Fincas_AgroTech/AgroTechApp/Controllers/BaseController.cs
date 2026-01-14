using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgroTechApp.Models.DB;
using System.Security.Claims;

namespace AgroTechApp.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly AgroTechDbContext _context;
        protected readonly ILogger _logger;

        public BaseController(AgroTechDbContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene la finca activa del usuario (de sesión o primera disponible)
        /// </summary>
        protected long GetFincaId()
        {
            try
            {
                // Verificar si hay finca en sesión
                var fincaIdEnSesion = HttpContext.Session.GetInt32("FincaActiva");

                if (fincaIdEnSesion.HasValue)
                {
                    // Validar que el usuario aún tiene acceso a esa finca
                    var tieneAcceso = ValidarAccesoAFinca(fincaIdEnSesion.Value);
                    if (tieneAcceso)
                    {
                        return fincaIdEnSesion.Value;
                    }
                }

                // Si no hay sesión, obtener primera finca disponible
                string? aspUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (aspUserId is null)
                {
                    _logger.LogWarning("Usuario no autenticado intentó acceder al sistema.");
                    throw new UnauthorizedAccessException("Usuario no autenticado.");
                }

                var userFinca = _context.UserFincas
                    .Include(uf => uf.Finca)
                    .AsNoTracking()
                    .Where(uf => uf.AspNetUserId == aspUserId && uf.Finca.Activa)
                    .OrderBy(uf => uf.FincaId) // Primera finca activa
                    .FirstOrDefault();

                if (userFinca is null)
                {
                    _logger.LogWarning($"El usuario {aspUserId} no tiene fincas asignadas.");
                    throw new InvalidOperationException(
                        "El usuario no tiene una finca asignada. Contacte al administrador.");
                }

                // Guardar en sesión para próximas peticiones
                HttpContext.Session.SetInt32("FincaActiva", (int)userFinca.FincaId);

                return userFinca.FincaId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo FincaId del usuario.");
                throw;
            }
        }

        /// <summary>
        /// Obtiene todas las fincas del usuario
        /// </summary>
        protected List<Finca> GetFincasUsuario()
        {
            try
            {
                string? aspUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (aspUserId is null)
                    throw new UnauthorizedAccessException("Usuario no autenticado.");

                var fincas = _context.UserFincas
                    .Include(uf => uf.Finca)
                    .Where(uf => uf.AspNetUserId == aspUserId && uf.Finca.Activa)
                    .Select(uf => uf.Finca)
                    .OrderBy(f => f.Nombre)
                    .ToList();

                return fincas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo fincas del usuario.");
                throw;
            }
        }

        /// <summary>
        /// Cambia la finca activa en sesión
        /// </summary>
        protected bool CambiarFincaActiva(long fincaId)
        {
            try
            {
                // Validar que el usuario tiene acceso a esa finca
                if (!ValidarAccesoAFinca(fincaId))
                {
                    _logger.LogWarning($"Usuario intentó acceder a finca {fincaId} sin permiso");
                    return false;
                }

                // Guardar en sesión
                HttpContext.Session.SetInt32("FincaActiva", (int)fincaId);

                _logger.LogInformation($"Usuario cambió finca activa a: {fincaId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cambiando finca activa a {fincaId}");
                return false;
            }
        }

        /// <summary>
        /// Valida que el usuario tiene acceso a una finca específica
        /// </summary>
        private bool ValidarAccesoAFinca(long fincaId)
        {
            try
            {
                string? aspUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (aspUserId is null)
                    return false;

                return _context.UserFincas
                    .Include(uf => uf.Finca)
                    .Any(uf => uf.AspNetUserId == aspUserId &&
                              uf.FincaId == fincaId &&
                              uf.Finca.Activa);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Valida que una entidad pertenezca a la finca del usuario
        /// </summary>
        protected void ValidarAcceso(long fincaId)
        {
            long userFincaId = GetFincaId();

            if (fincaId != userFincaId)
            {
                _logger.LogWarning(
                    $"Acceso denegado: Usuario (FincaId {userFincaId}) → Recurso (FincaId {fincaId})");

                throw new UnauthorizedAccessException("No tiene permisos para acceder a este recurso.");
            }
        }

        protected void ValidarAcceso(params long[] fincaIds)
        {
            long userFincaId = GetFincaId();

            foreach (long fId in fincaIds)
            {
                if (fId != userFincaId)
                {
                    _logger.LogWarning(
                        $"Acceso denegado: Usuario (FincaId {userFincaId}) → Recurso (FincaId {fId})");

                    throw new UnauthorizedAccessException("No tiene permisos para acceder a este recurso.");
                }
            }
        }

        protected string GetAspNetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                throw new UnauthorizedAccessException("Usuario no autenticado.");

            return userId;
        }

        protected List<long> GetFincasAsignadas()
        {
            var userId = GetAspNetUserId();

            var fincas = _context.UserFincas
                .Include(uf => uf.Finca)
                .AsNoTracking()
                .Where(uf => uf.AspNetUserId == userId && uf.Finca.Activa)
                .Select(uf => uf.FincaId)
                .ToList();

            if (!fincas.Any())
                throw new InvalidOperationException("El usuario no tiene fincas asignadas.");

            return fincas;
        }

        protected IActionResult ErrorView(string mensaje)
        {
            TempData["Error"] = mensaje;
            return RedirectToAction("Index", "Home");
        }

        protected void MostrarExito(string mensaje)
        {
            TempData["Success"] = mensaje;
        }

        protected void MostrarAdvertencia(string mensaje)
        {
            TempData["Warning"] = mensaje;
        }

        protected void MostrarError(string mensaje)
        {
            TempData["Error"] = mensaje;
        }


        protected void MostrarInfo(string mensaje)
        {
            TempData["InfoMessage"] = mensaje;
        }

        protected List<Finca> GetFincasUsuarioTodas()
        {
            var userId = GetAspNetUserId();

            var fincas = _context.UserFincas
                .Include(uf => uf.Finca)
                .AsNoTracking()
                .Where(uf => uf.AspNetUserId == userId)
                .Select(uf => uf.Finca)
                .OrderBy(f => f.Nombre)
                .ToList();

            return fincas;
        }

    }
}