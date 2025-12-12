using AgroTechApp.Models.DB;
using AgroTechApp.Services;
using AgroTechApp.Services.Inventario;
using AgroTechApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AgroTechApp.Models;

namespace AgroTechApp.Controllers
{
    [Authorize]
    public class InventarioController : BaseController
    {
        private readonly IInventarioService _inv;
        private readonly IFinanzasService _finanzasService;

        public InventarioController(
            AgroTechDbContext db,
            IInventarioService inv,
            IFinanzasService finanzasService,
            ILogger<InventarioController> logger)
            : base(db, logger)
        {
            _inv = inv;
            _finanzasService = finanzasService;
        }

        // ========= ENTRADA (GET) =========
        [HttpGet]
        public async Task<IActionResult> Entrada(long? insumoId = null, CancellationToken ct = default)
        {
            try
            {
                var fincaId = GetFincaId();

                await CargarCombosAsync(insumoId, fincaId, ct);

                return View(new EntradaVM
                {
                    FincaId = fincaId,
                    InsumoId = insumoId ?? 0
                });
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ========= ENTRADA (POST) =========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Entrada(EntradaVM vm, CancellationToken ct)
        {
            try
            {
                var fincaId = GetFincaId();
                vm.FincaId = fincaId;

                async Task Load() => await CargarCombosAsync(vm.InsumoId, fincaId, ct);

                if (!await _context.Insumos.AnyAsync(i => i.InsumoId == vm.InsumoId && i.FincaId == fincaId))
                {
                    ModelState.AddModelError("InsumoId", "El insumo no pertenece a su finca.");
                }

                if (!ModelState.IsValid)
                {
                    await Load();
                    return View(vm);
                }

                using var tx = await _context.Database.BeginTransactionAsync(ct);

                try
                {
                    long? loteId = await CrearOObtenerLoteOpcional(vm, fincaId, ct);

                    // Registro de entrada de inventario
                    await _inv.RegistrarEntradaAsync(
                        fincaId: fincaId,
                        insumoId: vm.InsumoId,
                        cantidad: vm.Cantidad,
                        costoUnitario: vm.PrecioUnitario,
                        loteId: loteId,
                        observacion: vm.Observaciones,
                        fecha: vm.Fecha ?? DateTime.UtcNow,
                        ct: ct
                    );

                    // Obtener el MovimientoInventario recien creado
                    var movimientoCreado = await _context.MovimientoInventarios
                        .Where(m => m.FincaId == fincaId && m.InsumoId == vm.InsumoId)
                        .OrderByDescending(m => m.MovId)
                        .FirstOrDefaultAsync(ct);

                    // Registrar gasto automatico SI tiene costo
                    if (vm.PrecioUnitario.HasValue && vm.PrecioUnitario.Value > 0 && movimientoCreado != null)
                    {
                        var insumo = await _context.Insumos
                            .Include(i => i.Unidad)
                            .FirstOrDefaultAsync(i => i.InsumoId == vm.InsumoId, ct);

                        if (insumo != null)
                        {
                            await _finanzasService.RegistrarGastoCompraInsumo(
                                fincaId: fincaId,
                                insumoId: vm.InsumoId,
                                nombreInsumo: insumo.Nombre,
                                cantidad: vm.Cantidad,
                                unidad: insumo.Unidad?.Codigo ?? insumo.Unidad?.Nombre ?? "unidad",
                                costoUnitario: vm.PrecioUnitario.Value,
                                fecha: vm.Fecha ?? DateTime.UtcNow,
                                movimientoId: movimientoCreado.MovId
                            );

                            _logger.LogInformation(
                                "Gasto automático registrado para entrada de {Insumo} por {Monto}",
                                insumo.Nombre, vm.Cantidad * vm.PrecioUnitario.Value);
                        }
                    }

                    await tx.CommitAsync(ct);
                    MostrarExito("Entrada registrada correctamente y gasto automático creado.");
                    return RedirectToAction("Index", "Insumoes");
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync(ct);
                    _logger.LogError(ex, "Error registrando entrada");
                    ModelState.AddModelError(string.Empty, "Ocurrió un error al registrar la entrada.");
                    await Load();
                    return View(vm);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ========= CONSUMO (GET) =========
        [HttpGet]
        public async Task<IActionResult> Consumo(long? insumoId = null, CancellationToken ct = default)
        {
            try
            {
                var fincaId = GetFincaId();

                await CargarCombosConsumoAsync(insumoId, fincaId, ct);

                var vm = new ConsumoVM
                {
                    FincaId = fincaId,
                    InsumoId = insumoId ?? 0,
                    Fecha = DateTime.Today
                };

                if (insumoId.HasValue && insumoId > 0)
                {
                    var stockDict = await _inv.GetStockPorInsumoAsync(fincaId, null, ct);
                    vm.StockDisponible = stockDict.TryGetValue(insumoId.Value, out var s) ? s : 0m;

                    var insumo = await _context.Insumos
                        .Include(i => i.Unidad)
                        .FirstOrDefaultAsync(i => i.InsumoId == insumoId && i.FincaId == fincaId, ct);

                    if (insumo != null)
                    {
                        vm.NombreInsumo = insumo.Nombre;
                        vm.Unidad = insumo.Unidad?.Codigo ?? insumo.Unidad?.Nombre;
                    }
                }

                return View(vm);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ========= CONSUMO (POST) =========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Consumo(ConsumoVM vm, CancellationToken ct)
        {
            try
            {
                var fincaId = GetFincaId();
                vm.FincaId = fincaId;

                async Task RecargarVista()
                {
                    await CargarCombosConsumoAsync(vm.InsumoId, fincaId, ct);
                    var stockDict = await _inv.GetStockPorInsumoAsync(fincaId, null, ct);
                    vm.StockDisponible = stockDict.TryGetValue(vm.InsumoId, out var s) ? s : 0m;
                }

                var insumo = await _context.Insumos
                    .Include(i => i.Unidad)
                    .FirstOrDefaultAsync(i => i.InsumoId == vm.InsumoId && i.FincaId == fincaId, ct);

                if (insumo == null)
                {
                    ModelState.AddModelError("InsumoId", "El insumo seleccionado no es válido.");
                    await RecargarVista();
                    return View(vm);
                }

                vm.NombreInsumo = insumo.Nombre;
                vm.Unidad = insumo.Unidad?.Codigo;

                var stockDict = await _inv.GetStockPorInsumoAsync(fincaId, null, ct);
                var stockActual = stockDict.TryGetValue(vm.InsumoId, out var stock) ? stock : 0m;
                vm.StockDisponible = stockActual;

                if (vm.Cantidad > stockActual)
                {
                    ModelState.AddModelError("Cantidad",
                        $"Stock insuficiente. Disponible: {stockActual:N2} {vm.Unidad}");
                    await RecargarVista();
                    return View(vm);
                }

                if (!ModelState.IsValid)
                {
                    await RecargarVista();
                    return View(vm);
                }

                using var tx = await _context.Database.BeginTransactionAsync(ct);

                try
                {
                    //Registrar consumo de inventario
                    await _inv.RegistrarConsumoAsync(
                        fincaId: fincaId,
                        insumoId: vm.InsumoId,
                        cantidad: vm.Cantidad,
                        loteId: null,
                        observacion: vm.Observaciones,
                        fecha: vm.Fecha ?? DateTime.UtcNow,
                        ct: ct
                    );

                    //Obtener el MovimientoInventario recien creado
                    var movimientoCreado = await _context.MovimientoInventarios
                        .Where(m => m.FincaId == fincaId &&
                                    m.InsumoId == vm.InsumoId &&
                                    m.TipoId == 2) // 2 = CONSUMO
                        .OrderByDescending(m => m.MovId)
                        .FirstOrDefaultAsync(ct);

                    //Registrar gasto automatico
                    if (movimientoCreado != null)
                    {
                        await _finanzasService.RegistrarGastoConsumoInsumo(
                            fincaId: fincaId,
                            insumoId: vm.InsumoId,
                            nombreInsumo: insumo.Nombre,
                            cantidad: vm.Cantidad,
                            unidad: insumo.Unidad?.Codigo ?? insumo.Unidad?.Nombre ?? "unidad",
                            fecha: vm.Fecha ?? DateTime.UtcNow,
                            movimientoId: movimientoCreado.MovId,
                            observacion: vm.Observaciones
                        );

                        _logger.LogInformation(
                            "Gasto automático de consumo registrado para {Insumo}",
                            insumo.Nombre);
                    }

                    await tx.CommitAsync(ct);
                    MostrarExito($"Consumo registrado: {vm.Cantidad:N2} {vm.Unidad} de {insumo.Nombre} y gasto automático creado.");
                    return RedirectToAction("Index", "Insumoes");
                }
                catch (InvalidOperationException ex)
                {
                    await tx.RollbackAsync(ct);
                    ModelState.AddModelError(string.Empty, ex.Message);
                    await RecargarVista();
                    return View(vm);
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync(ct);
                    _logger.LogError(ex, "Error registrando consumo");
                    ModelState.AddModelError(string.Empty, "Ocurrió un error al registrar el consumo.");
                    await RecargarVista();
                    return View(vm);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando consumo");
                ModelState.AddModelError(string.Empty, "Ocurrió un error al registrar el consumo.");
                return View(vm);
            }
        }

        // ========= DASHBOARD DE INVENTARIO =========
        [HttpGet]
        public async Task<IActionResult> Dashboard(CancellationToken ct = default)
        {
            try
            {
                var fincaId = GetFincaId();

                var insumos = await _context.Insumos
                    .AsNoTracking()
                    .Include(i => i.Categoria)
                    .Include(i => i.Unidad)
                    .Where(i => i.FincaId == fincaId)
                    .OrderBy(i => i.Nombre)
                    .ToListAsync(ct);

                var stockDict = await _inv.GetStockPorInsumoAsync(fincaId, null, ct);

                var (activos, porAcabarse, agotados, categorias) =
                    await _inv.GetKpisInsumosAsync(insumos, stockDict, ct);

                var ultimosMovimientos = await _context.MovimientoInventarios
                    .AsNoTracking()
                    .Include(m => m.Insumo)
                    .Include(m => m.Tipo)
                    .Where(m => m.FincaId == fincaId)
                    .OrderByDescending(m => m.Fecha)
                    .Take(10)
                    .ToListAsync(ct);

                var insumosBajoStock = insumos
                    .Where(i => i.Activo)
                    .Select(i => new
                    {
                        Insumo = i,
                        Stock = stockDict.TryGetValue(i.InsumoId, out var s) ? s : 0m
                    })
                    .Where(x => x.Stock <= x.Insumo.StockMinimo)
                    .OrderBy(x => x.Stock)
                    .Take(5)
                    .ToList();

                var finca = await _context.Fincas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.FincaId == fincaId, ct);

                ViewBag.FincaNombre = finca?.Nombre ?? $"Finca #{fincaId}";
                ViewBag.TotalActivos = activos;
                ViewBag.PorAcabarse = porAcabarse;
                ViewBag.Agotados = agotados;
                ViewBag.Categorias = categorias;
                ViewBag.StockPorInsumo = stockDict;
                ViewBag.UltimosMovimientos = ultimosMovimientos;
                ViewBag.InsumosBajoStock = insumosBajoStock;
                ViewBag.Insumos = insumos;

                return View();
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ========= HISTORIAL DE MOVIMIENTOS =========
        [HttpGet]
        public async Task<IActionResult> Historial(
            long? insumoId = null,
            string? tipo = null,
            DateTime? desde = null,
            DateTime? hasta = null,
            CancellationToken ct = default)
        {
            try
            {
                var fincaId = GetFincaId();

                var query = _context.MovimientoInventarios
                    .AsNoTracking()
                    .Include(m => m.Insumo)
                        .ThenInclude(i => i!.Unidad)
                    .Include(m => m.Tipo)
                    .Where(m => m.FincaId == fincaId);

                if (insumoId.HasValue && insumoId > 0)
                    query = query.Where(m => m.InsumoId == insumoId);

                if (!string.IsNullOrEmpty(tipo))
                    query = query.Where(m => m.Tipo!.Nombre == tipo);

                if (desde.HasValue)
                    query = query.Where(m => m.Fecha >= desde.Value);

                if (hasta.HasValue)
                    query = query.Where(m => m.Fecha <= hasta.Value.AddDays(1));

                var movimientos = await query
                    .OrderByDescending(m => m.Fecha)
                    .ThenByDescending(m => m.MovId)
                    .Take(100)
                    .ToListAsync(ct);

                ViewBag.Insumos = new SelectList(
                    await _context.Insumos
                        .Where(i => i.FincaId == fincaId)
                        .OrderBy(i => i.Nombre)
                        .Select(i => new { i.InsumoId, i.Nombre })
                        .ToListAsync(ct),
                    "InsumoId", "Nombre", insumoId);

                ViewBag.Tipos = new SelectList(
                    await _context.TipoMovimientoInventarios
                        .Select(t => t.Nombre)
                        .ToListAsync(ct),
                    tipo);

                ViewBag.InsumoIdFiltro = insumoId;
                ViewBag.TipoFiltro = tipo;
                ViewBag.DesdeFiltro = desde;
                ViewBag.HastaFiltro = hasta;

                var finca = await _context.Fincas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.FincaId == fincaId, ct);
                ViewBag.FincaNombre = finca?.Nombre ?? $"Finca #{fincaId}";

                return View(movimientos);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ========= AUTOCOMPLETE (JSON) =========
        [HttpGet]
        public async Task<IActionResult> BuscarInsumos(string term, CancellationToken ct)
        {
            try
            {
                var fincaId = GetFincaId();

                term = (term ?? "").Trim();

                var q = _context.Insumos
                    .AsNoTracking()
                    .Include(i => i.Categoria)
                    .Where(i => i.FincaId == fincaId && i.Activo);

                if (!string.IsNullOrEmpty(term))
                    q = q.Where(i => i.Nombre.Contains(term) ||
                                     i.Categoria!.Nombre.Contains(term));

                var data = await q
                    .OrderBy(i => i.Nombre)
                    .Take(20)
                    .Select(i => new
                    {
                        i.InsumoId,
                        Texto = i.Nombre + " (" + i.Categoria!.Nombre + ")"
                    })
                    .ToListAsync(ct);

                return Json(data);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }

        // ========= STOCK (JSON) =========
        [HttpGet]
        public async Task<IActionResult> Stock(long insumoId, CancellationToken ct)
        {
            try
            {
                var fincaId = GetFincaId();

                if (!await _context.Insumos.AnyAsync(i => i.InsumoId == insumoId && i.FincaId == fincaId))
                    return Unauthorized();

                var dict = await _inv.GetStockPorInsumoAsync(fincaId, null, ct);
                var stock = dict.TryGetValue(insumoId, out var s) ? s : 0m;

                return Json(new { stock });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }

        // ========= Helpers =========
        private async Task CargarCombosAsync(long? insumoId, long fincaId, CancellationToken ct)
        {
            ViewData["FincaNombre"] =
                await _context.Fincas
                    .Where(f => f.FincaId == fincaId)
                    .Select(f => f.Nombre)
                    .FirstOrDefaultAsync(ct)
                ?? $"Finca #{fincaId}";

            var insumosBase = await _context.Insumos
                .AsNoTracking()
                .Include(i => i.Categoria)
                .Where(i => i.FincaId == fincaId)
                .OrderBy(i => i.Nombre)
                .Take(15)
                .Select(i => new
                {
                    i.InsumoId,
                    Texto = i.Nombre + " (" + i.Categoria!.Nombre + ")"
                })
                .ToListAsync(ct);

            ViewData["InsumoSelect"] = new SelectList(insumosBase, "InsumoId", "Texto", insumoId);
        }

        private async Task CargarCombosConsumoAsync(long? insumoId, long fincaId, CancellationToken ct)
        {
            ViewData["FincaNombre"] =
                await _context.Fincas
                    .Where(f => f.FincaId == fincaId)
                    .Select(f => f.Nombre)
                    .FirstOrDefaultAsync(ct)
                ?? $"Finca #{fincaId}";

            var stockDict = await _inv.GetStockPorInsumoAsync(fincaId, null, ct);

            var insumosConStock = await _context.Insumos
                .AsNoTracking()
                .Include(i => i.Categoria)
                .Include(i => i.Unidad)
                .Where(i => i.FincaId == fincaId && i.Activo)
                .OrderBy(i => i.Nombre)
                .ToListAsync(ct);

            var listaInsumos = insumosConStock
                .Select(i => new
                {
                    i.InsumoId,
                    Texto = $"{i.Nombre} ({i.Categoria?.Nombre}) - Stock: {(stockDict.TryGetValue(i.InsumoId, out var s) ? s : 0):N2} {i.Unidad?.Codigo}"
                })
                .ToList();

            ViewData["InsumoSelect"] = new SelectList(listaInsumos, "InsumoId", "Texto", insumoId);
        }

        private async Task<long?> CrearOObtenerLoteOpcional(EntradaVM vm, long fincaId, CancellationToken ct)
        {
            if (!vm.UsarLote) return null;

            var insumo = await _context.Insumos
                .FirstOrDefaultAsync(i => i.InsumoId == vm.InsumoId && i.FincaId == fincaId, ct);

            if (insumo == null)
                throw new UnauthorizedAccessException("Intento de crear lote de otro usuario.");

            if (!string.IsNullOrWhiteSpace(vm.CodigoLote))
            {
                var code = vm.CodigoLote.Trim();

                var existente = await _context.InsumoLotes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.InsumoId == vm.InsumoId &&
                        x.CodigoLote == code, ct);

                if (existente != null) return existente.LoteId;

                var nuevo = new InsumoLote
                {
                    InsumoId = vm.InsumoId,
                    CodigoLote = code,
                    FechaVencimiento = vm.FechaVencimiento.HasValue
                        ? DateOnly.FromDateTime(vm.FechaVencimiento.Value)
                        : (DateOnly?)null
                };

                _context.InsumoLotes.Add(nuevo);
                await _context.SaveChangesAsync(ct);
                return nuevo.LoteId;
            }

            if (vm.FechaVencimiento.HasValue)
            {
                var fv = DateOnly.FromDateTime(vm.FechaVencimiento.Value);

                var existente = await _context.InsumoLotes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.InsumoId == vm.InsumoId &&
                        x.CodigoLote == null &&
                        x.FechaVencimiento == fv, ct);

                if (existente != null) return existente.LoteId;

                var nuevo = new InsumoLote
                {
                    InsumoId = vm.InsumoId,
                    CodigoLote = null,
                    FechaVencimiento = fv
                };

                _context.InsumoLotes.Add(nuevo);
                await _context.SaveChangesAsync(ct);
                return nuevo.LoteId;
            }

            return null;
        }
    }
}