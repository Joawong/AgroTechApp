using System.Diagnostics;
using AgroTechApp.Models;
using AgroTechApp.Models.DB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroTechApp.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(
            AgroTechDbContext context,
            ILogger<HomeController> logger)
            : base(context, logger)
        {
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // ========================================================================
        // NUEVO: Cambiar Finca Activa
        // ========================================================================

        [Authorize]
        [HttpPost]
        public IActionResult CambiarFinca(long fincaId)
        {
            try
            {
                bool cambioExitoso = CambiarFincaActiva(fincaId);

                if (cambioExitoso)
                {
                    return Json(new { success = true, message = "Finca cambiada exitosamente" });
                }
                else
                {
                    return Json(new { success = false, message = "No tiene acceso a esa finca" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar de finca");
                return Json(new { success = false, message = "Error al cambiar de finca" });
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult GetFincaActual()
        {
            try
            {
                long fincaId = GetFincaId();
                var finca = _context.Fincas.Find(fincaId);

                return Json(new
                {
                    success = true,
                    fincaId = fincaId,
                    nombre = finca?.Nombre ?? "N/A"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo finca actual");
                return Json(new { success = false });
            }
        }
    }
}