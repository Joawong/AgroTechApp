using AgroTechApp.Models.DB;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.IO;
using System.Text.Json;

namespace AgroTechApp.Controllers
{
    [Authorize]
    public class FinanzasController : BaseController
    {
        public FinanzasController(
            AgroTechDbContext context,
            ILogger<FinanzasController> logger)
            : base(context, logger)
        {
        }

        public async Task<IActionResult> Dashboard(
            string? periodo,
            DateTime? fechaDesde,
            DateTime? fechaHasta,
            long? animalId,
            long? potreroId,
            int? categoriaId)
        {
            try
            {
                var fincaId = GetFincaId();

                // Determinar rango de fechas
                DateTime inicio, fin;
                string periodoTexto;

                if (fechaDesde.HasValue && fechaHasta.HasValue)
                {
                    inicio = fechaDesde.Value;
                    fin = fechaHasta.Value;
                    periodoTexto = $"{inicio:dd/MM/yyyy} - {fin:dd/MM/yyyy}";
                    periodo = "custom";
                }
                else
                {
                    (inicio, fin, periodoTexto) = ObtenerRangoPeriodo(periodo ?? "mes");
                }

                // Convertir a DateOnly para comparación
                var inicioDate = DateOnly.FromDateTime(inicio);
                var finDate = DateOnly.FromDateTime(fin);

                // ====== PERÍODO ACTUAL ======
                var gastosQuery = _context.Gastos
                    .Where(g => g.FincaId == fincaId &&
                               g.Fecha >= inicioDate &&
                               g.Fecha <= finDate);

                var ingresosQuery = _context.Ingresos
                    .Where(i => i.FincaId == fincaId &&
                               i.Fecha >= inicioDate &&
                               i.Fecha <= finDate);

                // Aplicar filtros adicionales
                if (animalId.HasValue)
                {
                    gastosQuery = gastosQuery.Where(g => g.AnimalId == animalId.Value);
                    ingresosQuery = ingresosQuery.Where(i => i.AnimalId == animalId.Value);
                }

                if (potreroId.HasValue)
                    gastosQuery = gastosQuery.Where(g => g.PotreroId == potreroId.Value);

                if (categoriaId.HasValue)
                    gastosQuery = gastosQuery.Where(g => g.RubroGastoId == categoriaId.Value);

                var gastos = await gastosQuery
                    .Include(g => g.RubroGasto)
                    .Include(g => g.Animal)
                    .Include(g => g.Insumo)
                    .ToListAsync();

                var ingresos = await ingresosQuery
                    .Include(i => i.RubroIngreso)
                    .Include(i => i.Animal)
                    .ToListAsync();

                // ====== PERÍODO ANTERIOR (para comparación) ======
                var diasPeriodo = (fin - inicio).Days + 1;
                var inicioAnterior = inicio.AddDays(-diasPeriodo);
                var finAnterior = inicio.AddDays(-1);
                var inicioAnteriorDate = DateOnly.FromDateTime(inicioAnterior);
                var finAnteriorDate = DateOnly.FromDateTime(finAnterior);

                var gastosAnteriores = await _context.Gastos
                    .Where(g => g.FincaId == fincaId &&
                               g.Fecha >= inicioAnteriorDate &&
                               g.Fecha <= finAnteriorDate)
                    .ToListAsync();

                var ingresosAnteriores = await _context.Ingresos
                    .Where(i => i.FincaId == fincaId &&
                               i.Fecha >= inicioAnteriorDate &&
                               i.Fecha <= finAnteriorDate)
                    .ToListAsync();

                // KPIs principales
                var totalIngresos = ingresos.Sum(i => i.Monto);
                var totalGastos = gastos.Sum(g => g.Monto);
                var utilidadNeta = totalIngresos - totalGastos;
                var margenUtilidad = totalIngresos > 0 ? (utilidadNeta / totalIngresos) * 100 : 0;

                ViewBag.TotalIngresos = totalIngresos;
                ViewBag.TotalGastos = totalGastos;
                ViewBag.UtilidadNeta = utilidadNeta;
                ViewBag.MargenUtilidad = margenUtilidad;
                ViewBag.CantidadIngresos = ingresos.Count;
                ViewBag.CantidadGastos = gastos.Count;
                ViewBag.PeriodoTexto = periodoTexto;
                ViewBag.PeriodoActual = periodo;
                ViewBag.FechaDesde = inicio.ToString("yyyy-MM-dd");
                ViewBag.FechaHasta = fin.ToString("yyyy-MM-dd");

                // ====== COMPARACIÓN CON PERÍODO ANTERIOR ======
                var totalIngresosAnterior = ingresosAnteriores.Sum(i => i.Monto);
                var totalGastosAnterior = gastosAnteriores.Sum(g => g.Monto);

                ViewBag.VariacionIngresos = totalIngresosAnterior > 0
                    ? ((totalIngresos - totalIngresosAnterior) / totalIngresosAnterior) * 100
                    : 0;

                ViewBag.VariacionGastos = totalGastosAnterior > 0
                    ? ((totalGastos - totalGastosAnterior) / totalGastosAnterior) * 100
                    : 0;

                ViewBag.TotalIngresosAnterior = totalIngresosAnterior;
                ViewBag.TotalGastosAnterior = totalGastosAnterior;

                // Generar alertas
                ViewBag.Alertas = GenerarAlertas(gastos, ingresos, totalGastos, totalIngresos, margenUtilidad);

                // Top 10 gastos (ARREGLADO - sin cast)
                ViewBag.TopGastos = gastos
                    .GroupBy(g => g.RubroGasto.Nombre)
                    .Select(g => new
                    {
                        Rubro = g.Key,
                        Monto = g.Sum(x => x.Monto),
                        Porcentaje = totalGastos > 0 ? (g.Sum(x => x.Monto) / totalGastos) * 100 : 0
                    })
                    .OrderByDescending(g => g.Monto)
                    .Take(10)
                    .ToList();

                // Top 10 ingresos (ARREGLADO - sin cast)
                ViewBag.TopIngresos = ingresos
                    .GroupBy(i => i.RubroIngreso.Nombre)
                    .Select(i => new
                    {
                        Rubro = i.Key,
                        Monto = i.Sum(x => x.Monto),
                        Porcentaje = totalIngresos > 0 ? (i.Sum(x => x.Monto) / totalIngresos) * 100 : 0
                    })
                    .OrderByDescending(i => i.Monto)
                    .Take(10)
                    .ToList();

                // Datos para gráficos
                ViewBag.TendenciaJson = GenerarDatosTendencia(gastos, ingresos, inicio, fin);
                ViewBag.GastosRubroJson = GenerarDatosRubros(gastos.GroupBy(g => g.RubroGasto.Nombre));
                ViewBag.IngresosRubroJson = GenerarDatosRubros(ingresos.GroupBy(i => i.RubroIngreso.Nombre));

                // ====== ANÁLISIS POR CATEGORÍA ======
                ViewBag.GastosPorOrigen = gastos
                    .GroupBy(g => g.OrigenModulo ?? "Manual")
                    .Select(g => new
                    {
                        Origen = g.Key,
                        Monto = g.Sum(x => x.Monto),
                        Cantidad = g.Count()
                    })
                    .OrderByDescending(g => g.Monto)
                    .ToList();

                // ====== DROPDOWNS PARA FILTROS ======
                ViewBag.AnimalesDropdown = await _context.Animals
                    .Where(a => a.FincaId == fincaId && a.Estado == "Activo")
                    .Select(a => new { a.AnimalId, Display = $"{a.Arete} - {a.Nombre}" })
                    .ToListAsync();

                ViewBag.PotrerosDropdown = await _context.Potreros
                    .Where(p => p.FincaId == fincaId)
                    .Select(p => new { p.PotreroId, p.Nombre })
                    .ToListAsync();

                ViewBag.RubrosGastoDropdown = await _context.RubroGastos
                    .Select(r => new { r.RubroGastoId, r.Nombre })
                    .ToListAsync();

                ViewBag.AnimalIdFiltro = animalId;
                ViewBag.PotreroIdFiltro = potreroId;
                ViewBag.CategoriaIdFiltro = categoriaId;

                return View();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ====== EXPORTAR A EXCEL ======
        public async Task<IActionResult> ExportarExcel(
            string? periodo,
            DateTime? fechaDesde,
            DateTime? fechaHasta)
        {
            try
            {
                var fincaId = GetFincaId();

                // Determinar rango de fechas
                DateTime inicio, fin;
                if (fechaDesde.HasValue && fechaHasta.HasValue)
                {
                    inicio = fechaDesde.Value;
                    fin = fechaHasta.Value;
                }
                else
                {
                    (inicio, fin, _) = ObtenerRangoPeriodo(periodo ?? "mes");
                }

                var inicioDate = DateOnly.FromDateTime(inicio);
                var finDate = DateOnly.FromDateTime(fin);

                // Obtener datos
                var gastos = await _context.Gastos
                    .Where(g => g.FincaId == fincaId &&
                               g.Fecha >= inicioDate &&
                               g.Fecha <= finDate)
                    .Include(g => g.RubroGasto)
                    .Include(g => g.Animal)
                    .Include(g => g.Insumo)
                    .OrderByDescending(g => g.Fecha)
                    .ToListAsync();

                var ingresos = await _context.Ingresos
                    .Where(i => i.FincaId == fincaId &&
                               i.Fecha >= inicioDate &&
                               i.Fecha <= finDate)
                    .Include(i => i.RubroIngreso)
                    .Include(i => i.Animal)
                    .OrderByDescending(i => i.Fecha)
                    .ToListAsync();

                // Crear Excel
                using var workbook = new XLWorkbook();

                // Hoja 1: Resumen
                var wsResumen = workbook.Worksheets.Add("Resumen");
                wsResumen.Cell("A1").Value = "REPORTE FINANCIERO";
                wsResumen.Cell("A2").Value = $"Período: {inicio:dd/MM/yyyy} - {fin:dd/MM/yyyy}";
                wsResumen.Cell("A4").Value = "KPI";
                wsResumen.Cell("B4").Value = "Valor";
                wsResumen.Cell("A5").Value = "Total Ingresos";
                wsResumen.Cell("B5").Value = ingresos.Sum(i => i.Monto);
                wsResumen.Cell("A6").Value = "Total Gastos";
                wsResumen.Cell("B6").Value = gastos.Sum(g => g.Monto);
                wsResumen.Cell("A7").Value = "Utilidad Neta";
                wsResumen.Cell("B7").Value = ingresos.Sum(i => i.Monto) - gastos.Sum(g => g.Monto);

                // Hoja 2: Gastos
                var wsGastos = workbook.Worksheets.Add("Gastos");
                wsGastos.Cell("A1").Value = "Fecha";
                wsGastos.Cell("B1").Value = "Rubro";
                wsGastos.Cell("C1").Value = "Monto";
                wsGastos.Cell("D1").Value = "Descripción";
                wsGastos.Cell("E1").Value = "Animal";
                wsGastos.Cell("F1").Value = "Origen";

                int rowGasto = 2;
                foreach (var gasto in gastos)
                {
                    wsGastos.Cell($"A{rowGasto}").Value = gasto.Fecha.ToString("dd/MM/yyyy");
                    wsGastos.Cell($"B{rowGasto}").Value = gasto.RubroGasto.Nombre;
                    wsGastos.Cell($"C{rowGasto}").Value = gasto.Monto;
                    wsGastos.Cell($"D{rowGasto}").Value = gasto.Descripcion ?? "";
                    wsGastos.Cell($"E{rowGasto}").Value = gasto.Animal?.Arete ?? "";
                    wsGastos.Cell($"F{rowGasto}").Value = gasto.EsAutomatico ? "Automático" : "Manual";
                    rowGasto++;
                }

                // Hoja 3: Ingresos
                var wsIngresos = workbook.Worksheets.Add("Ingresos");
                wsIngresos.Cell("A1").Value = "Fecha";
                wsIngresos.Cell("B1").Value = "Rubro";
                wsIngresos.Cell("C1").Value = "Monto";
                wsIngresos.Cell("D1").Value = "Descripción";
                wsIngresos.Cell("E1").Value = "Animal";
                wsIngresos.Cell("F1").Value = "Origen";

                int rowIngreso = 2;
                foreach (var ingreso in ingresos)
                {
                    wsIngresos.Cell($"A{rowIngreso}").Value = ingreso.Fecha.ToString("dd/MM/yyyy");
                    wsIngresos.Cell($"B{rowIngreso}").Value = ingreso.RubroIngreso.Nombre;
                    wsIngresos.Cell($"C{rowIngreso}").Value = ingreso.Monto;
                    wsIngresos.Cell($"D{rowIngreso}").Value = ingreso.Descripcion ?? "";
                    wsIngresos.Cell($"E{rowIngreso}").Value = ingreso.Animal?.Arete ?? "";
                    wsIngresos.Cell($"F{rowIngreso}").Value = ingreso.EsAutomatico ? "Automático" : "Manual";
                    rowIngreso++;
                }

                // Aplicar formato
                foreach (var ws in workbook.Worksheets)
                {
                    ws.Columns().AdjustToContents();
                    ws.Row(1).Style.Font.Bold = true;
                    ws.Row(1).Style.Fill.BackgroundColor = XLColor.LightGreen;
                }

                // Guardar en memoria
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var content = stream.ToArray();

                var fileName = $"ReporteFinanciero_{inicio:yyyyMMdd}_{fin:yyyyMMdd}.xlsx";
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar Excel");
                TempData["Error"] = "Error al generar el reporte";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        // ====== MÉTODOS AUXILIARES ======

        private (DateTime inicio, DateTime fin, string texto) ObtenerRangoPeriodo(string periodo)
        {
            var hoy = DateTime.Today;

            return periodo.ToLower() switch
            {
                "hoy" => (hoy, hoy, "Hoy"),
                "semana" => (hoy.AddDays(-(int)hoy.DayOfWeek), hoy, "Esta Semana"),
                "mes" => (new DateTime(hoy.Year, hoy.Month, 1),
                         new DateTime(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month)),
                         "Este Mes"),
                "trimestre" => (hoy.AddMonths(-3), hoy, "Último Trimestre"),
                "anio" => (new DateTime(hoy.Year, 1, 1), new DateTime(hoy.Year, 12, 31), "Este Año"),
                _ => (new DateTime(hoy.Year, hoy.Month, 1),
                     new DateTime(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month)),
                     "Este Mes")
            };
        }

        private List<dynamic> GenerarAlertas(
            List<Gasto> gastos,
            List<Ingreso> ingresos,
            decimal totalGastos,
            decimal totalIngresos,
            decimal margenUtilidad)
        {
            var alertas = new List<dynamic>();

            // Alerta si los gastos superan los ingresos
            if (totalGastos > totalIngresos)
            {
                alertas.Add(new
                {
                    Tipo = "danger",
                    Icono = "exclamation-circle",
                    Titulo = "Gastos Superan Ingresos",
                    Mensaje = $"Los gastos (₡{totalGastos:N0}) son mayores que los ingresos (₡{totalIngresos:N0}). Déficit: ₡{(totalGastos - totalIngresos):N0}"
                });
            }

            // Alerta si no hay movimientos
            if (gastos.Count == 0 && ingresos.Count == 0)
            {
                alertas.Add(new
                {
                    Tipo = "info",
                    Icono = "info-circle",
                    Titulo = "Sin Movimientos",
                    Mensaje = "No hay registros financieros en este período"
                });
            }

            // Alerta si margen es bajo
            if (margenUtilidad < 10 && margenUtilidad > 0)
            {
                alertas.Add(new
                {
                    Tipo = "warning",
                    Icono = "exclamation-triangle",
                    Titulo = "Margen de Utilidad Bajo",
                    Mensaje = $"El margen de utilidad es de {margenUtilidad:N1}%. Se recomienda optimizar gastos o aumentar ingresos."
                });
            }

            return alertas;
        }

        private string GenerarDatosTendencia(
            List<Gasto> gastos,
            List<Ingreso> ingresos,
            DateTime inicio,
            DateTime fin)
        {
            var dias = (fin - inicio).Days + 1;
            var labels = new List<string>();
            var dataIngresos = new List<decimal>();
            var dataGastos = new List<decimal>();

            for (int i = 0; i < dias; i++)
            {
                var fecha = DateOnly.FromDateTime(inicio.AddDays(i));
                labels.Add(fecha.ToString("dd/MM"));

                dataIngresos.Add(ingresos.Where(x => x.Fecha == fecha).Sum(x => x.Monto));
                dataGastos.Add(gastos.Where(x => x.Fecha == fecha).Sum(x => x.Monto));
            }

            return JsonSerializer.Serialize(new
            {
                labels,
                ingresos = dataIngresos,
                gastos = dataGastos
            });
        }

        private string GenerarDatosRubros<T>(IEnumerable<IGrouping<string, T>> grupos) where T : class
        {
            var labels = new List<string>();
            var valores = new List<decimal>();

            foreach (var grupo in grupos.OrderByDescending(g => g.Count()).Take(8))
            {
                labels.Add(grupo.Key);

                decimal suma = 0;
                if (typeof(T) == typeof(Gasto))
                {
                    suma = grupo.Cast<Gasto>().Sum(x => x.Monto);
                }
                else if (typeof(T) == typeof(Ingreso))
                {
                    suma = grupo.Cast<Ingreso>().Sum(x => x.Monto);
                }

                valores.Add(suma);
            }

            return JsonSerializer.Serialize(new { labels, valores });
        }
    }
}