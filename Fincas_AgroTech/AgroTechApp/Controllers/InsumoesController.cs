using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using AgroTechApp.Models.DB;
using AgroTechApp.Services.Inventario;
using AgroTechApp.ViewModels.Insumo;

namespace AgroTechApp.Controllers
{
    [Authorize]
    public class InsumoesController : BaseController
    {
        private readonly IInventarioService _inv;

        public InsumoesController(
            AgroTechDbContext context,
            ILogger<InsumoesController> logger,
            IInventarioService inv)
            : base(context, logger)
        {
            _inv = inv;
        }

        // GET: Insumoes
        public async Task<IActionResult> Index(
            long? fincaId = null,            // se ignorará y se usará la finca del usuario
            int? categoriaId = null,
            string? estado = null,           // "activo" | "inactivo" | "bajo" | "agotado"
            string? q = null,                // búsqueda por nombre o categoría
            CancellationToken ct = default)
        {
            try
            {
                var usuarioFincaId = GetFincaId();

                // Base query + includes
                var query = _context.Insumos
                    .AsNoTracking()
                    .Include(i => i.Categoria)
                    .Include(i => i.Unidad)
                    .Include(i => i.Finca)
                    .Where(i => i.FincaId == usuarioFincaId) // ✅ filtro multi-tenant
                    .AsQueryable();

                if (categoriaId.HasValue && categoriaId > 0)
                    query = query.Where(i => i.CategoriaId == categoriaId);

                if (!string.IsNullOrWhiteSpace(q))
                {
                    var term = q.Trim();
                    query = query.Where(i =>
                        i.Nombre.Contains(term) ||
                        i.Categoria!.Nombre.Contains(term));
                }

                // Lista provisional para KPIs y stock
                var insumos = await query.OrderBy(i => i.Nombre).ToListAsync(ct);

                // Stock actual por insumo (solo finca del usuario)
                var stockDict = await _inv.GetStockPorInsumoAsync(usuarioFincaId, null, ct);

                // Filtro por estado
                if (!string.IsNullOrEmpty(estado))
                {
                    estado = estado.ToLowerInvariant();
                    insumos = estado switch
                    {
                        "activo" => insumos.Where(i => i.Activo).ToList(),
                        "inactivo" => insumos.Where(i => !i.Activo).ToList(),
                        "bajo" => insumos.Where(i =>
                            (stockDict.TryGetValue(i.InsumoId, out var s) ? s : 0m) > 0 &&
                            (stockDict.TryGetValue(i.InsumoId, out var s2) ? s2 : 0m) <= i.StockMinimo).ToList(),
                        "agotado" => insumos.Where(i =>
                            (stockDict.TryGetValue(i.InsumoId, out var s) ? s : 0m) == 0m).ToList(),
                        _ => insumos
                    };
                }

                // KPIs
                var (activos, porAcabarse, agotados, categorias) =
                    await _inv.GetKpisInsumosAsync(insumos, stockDict, ct);

                ViewBag.TotalActivos = activos;
                ViewBag.PorAcabarse = porAcabarse;
                ViewBag.Agotados = agotados;
                ViewBag.Categorias = categorias;
                ViewBag.StockPorInsumo = stockDict;

                // Filtros (finca al final es solo informativo, no editable)
                var finca = await _context.Fincas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.FincaId == usuarioFincaId, ct);

                ViewBag.FincaNombre = finca?.Nombre ?? $"Finca #{usuarioFincaId}";

                ViewBag.FincaId = new SelectList(
                    new[]
                    {
                        new { FincaId = usuarioFincaId, Nombre = finca?.Nombre ?? $"Finca #{usuarioFincaId}" }
                    },
                    "FincaId", "Nombre", usuarioFincaId);

                ViewBag.CategoriaId = new SelectList(
                    await _context.CategoriaInsumos.AsNoTracking()
                        .Select(c => new { c.CategoriaId, c.Nombre })
                        .ToListAsync(ct),
                    "CategoriaId", "Nombre", categoriaId);

                ViewBag.Estado = estado;
                ViewBag.Q = q;

                return View(insumos);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado en Insumoes.Index");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var fincaId = GetFincaId();

                var insumo = await _context.Insumos
                    .Include(i => i.Categoria)
                    .Include(i => i.Finca)
                    .Include(i => i.Unidad)
                    .FirstOrDefaultAsync(m => m.InsumoId == id && m.FincaId == fincaId);

                if (insumo == null)
                    return NotFound();

                // ✅ CALCULAR STOCK ACTUAL usando el servicio de inventario
                var stockDict = await _inv.GetStockPorInsumoAsync(fincaId, id, CancellationToken.None);
                var stockActual = stockDict.TryGetValue(insumo.InsumoId, out var stock) ? stock : 0m;

                // ✅ OBTENER ACTIVIDAD RECIENTE (últimos 5 movimientos)
                var actividadReciente = await _context.MovimientoInventarios
                    .Where(m => m.InsumoId == id && m.FincaId == fincaId)
                    .OrderByDescending(m => m.Fecha)
                    .Take(5)
                    .Select(m => new
                    {
                        m.Tipo,
                        m.Fecha,
                        m.Cantidad
                    })
                    .ToListAsync();

                // ✅ CONTAR TOTAL DE MOVIMIENTOS
                var totalMovimientos = await _context.MovimientoInventarios
                    .Where(m => m.InsumoId == id && m.FincaId == fincaId)
                    .CountAsync();

                // ✅ OBTENER ÚLTIMO MOVIMIENTO
                var ultimoMovimiento = await _context.MovimientoInventarios
                    .Where(m => m.InsumoId == id && m.FincaId == fincaId)
                    .OrderByDescending(m => m.Fecha)
                    .Select(m => m.Fecha)
                    .FirstOrDefaultAsync();

                // ✅ PASAR DATOS A LA VISTA
                ViewBag.StockActual = stockActual;
                ViewBag.ActividadReciente = actividadReciente;
                ViewBag.TotalMovimientos = totalMovimientos;
                ViewBag.UltimoMovimiento = ultimoMovimiento;

                return View(insumo);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado en Insumoes.Details");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // GET: Insumoes/Create
        public IActionResult Create()
        {
            try
            {
                var fincaId = GetFincaId();

                // Finca no se selecciona, está fija
                ViewData["FincaNombre"] = _context.Fincas
                    .AsNoTracking()
                    .FirstOrDefault(f => f.FincaId == fincaId)?.Nombre ?? $"Finca #{fincaId}";

                ViewData["CategoriaId"] = new SelectList(
                    _context.CategoriaInsumos.AsNoTracking()
                        .Select(c => new { c.CategoriaId, c.Nombre }),
                    "CategoriaId", "Nombre");

                ViewData["UnidadId"] = new SelectList(
                    _context.UnidadMedida.AsNoTracking()
                        .Select(u => new { u.UnidadId, Texto = u.Codigo + " - " + u.Nombre }),
                    "UnidadId", "Texto");

                var vm = new InsumoCreateVM { FechaIngreso = DateTime.Today };
                return View(vm);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado en Insumoes.Create (GET)");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // POST: Insumoes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InsumoCreateVM vm, CancellationToken ct)
        {
            try
            {
                var fincaId = GetFincaId();       // ✅ fija la finca
                vm.FincaId = fincaId;             // ignoramos cualquier valor que venga del cliente
                // ✅ CORRECCIÓN: Remover validaciones de navegación
                ModelState.Remove("FincaId");
                ModelState.Remove("Finca");
                ModelState.Remove("Categoria");
                ModelState.Remove("Unidad");


                void LoadSelects()
                {
                    ViewData["FincaNombre"] = _context.Fincas
                        .AsNoTracking()
                        .FirstOrDefault(f => f.FincaId == fincaId)?.Nombre ?? $"Finca #{fincaId}";

                    ViewData["CategoriaId"] = new SelectList(
                        _context.CategoriaInsumos.AsNoTracking()
                            .Select(c => new { c.CategoriaId, c.Nombre }),
                        "CategoriaId", "Nombre", vm.CategoriaId);

                    ViewData["UnidadId"] = new SelectList(
                        _context.UnidadMedida.AsNoTracking()
                            .Select(u => new { u.UnidadId, Texto = u.Codigo + " - " + u.Nombre }),
                        "UnidadId", "Texto", vm.UnidadId);
                }

                if (!ModelState.IsValid)
                {
                    LoadSelects();
                    return View(vm);
                }

                using var tx = await _context.Database.BeginTransactionAsync(ct);
                try
                {
                    // 1) Crear Insumo
                    var insumo = new Insumo
                    {
                        Nombre = vm.Nombre.Trim(),
                        FincaId = fincaId,           // ✅ aseguramos finca del usuario
                        CategoriaId = vm.CategoriaId,
                        UnidadId = vm.UnidadId,
                        StockMinimo = vm.StockMinimo,
                        Activo = vm.Activo
                    };
                    _context.Insumos.Add(insumo);
                    await _context.SaveChangesAsync(ct);

                    // 2) Lote opcional
                    long? loteId = null;
                    if (!string.IsNullOrWhiteSpace(vm.CodigoLote) || vm.FechaVencimiento.HasValue)
                    {
                        var lote = new InsumoLote
                        {
                            InsumoId = insumo.InsumoId,
                            CodigoLote = string.IsNullOrWhiteSpace(vm.CodigoLote)
                                ? null
                                : vm.CodigoLote.Trim(),
                            FechaVencimiento = vm.FechaVencimiento.HasValue
                                ? DateOnly.FromDateTime(vm.FechaVencimiento.Value)
                                : (DateOnly?)null
                        };
                        _context.InsumoLotes.Add(lote);
                        await _context.SaveChangesAsync(ct);
                        loteId = lote.LoteId;
                    }

                    // 3) Movimiento de entrada inicial
                    if (vm.RegistrarEntradaInicial && vm.Cantidad is > 0 && vm.FechaIngreso.HasValue)
                    {
                        await _inv.RegistrarEntradaAsync(
                            fincaId: fincaId,
                            insumoId: insumo.InsumoId,
                            cantidad: vm.Cantidad.Value,
                            costoUnitario: vm.PrecioUnitario,
                            loteId: loteId,
                            observacion: vm.Observaciones,
                            fecha: vm.FechaIngreso.Value,
                            ct: ct
                        );
                    }

                    await tx.CommitAsync(ct);
                    MostrarExito("Insumo creado correctamente.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync(ct);
                    _logger.LogError(ex, "Error al crear el insumo");
                    ModelState.AddModelError(string.Empty, "Ocurrió un error al crear el insumo.");
                    LoadSelects();
                    return View(vm);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado en Insumoes.Create (POST)");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // GET: Insumoes/Edit/5
        public async Task<IActionResult> Edit(long? id, CancellationToken ct)
        {
            if (id is null) return NotFound();

            try
            {
                var fincaId = GetFincaId();

                var insumo = await _context.Insumos
                    .Include(i => i.Finca)
                    .Include(i => i.Categoria)
                    .Include(i => i.Unidad)
                    .FirstOrDefaultAsync(i => i.InsumoId == id && i.FincaId == fincaId, ct); // ✅ filtro

                if (insumo is null) return NotFound();

                // Combos (finca no editable)
                ViewBag.CategoriaId = new SelectList(
                    _context.CategoriaInsumos.AsNoTracking()
                        .OrderBy(c => c.Nombre)
                        .Select(c => new { c.CategoriaId, c.Nombre }),
                    "CategoriaId", "Nombre", insumo.CategoriaId
                );

                ViewBag.UnidadId = new SelectList(
                    _context.UnidadMedida.AsNoTracking()
                        .OrderBy(u => u.Codigo).ThenBy(u => u.Nombre)
                        .Select(u => new { u.UnidadId, Texto = u.Codigo + " - " + u.Nombre }),
                    "UnidadId", "Texto", insumo.UnidadId
                );

                ViewBag.FincaNombre = insumo.Finca?.Nombre ?? $"Finca #{insumo.FincaId}";
                ViewBag.CategoriaActual = insumo.Categoria?.Nombre ?? $"#{insumo.CategoriaId}";
                ViewBag.UnidadActual = insumo.Unidad is null
                    ? $"#{insumo.UnidadId}"
                    : $"{insumo.Unidad.Codigo} - {insumo.Unidad.Nombre}";

                // Métricas de inventario
                var stockDict = await _inv.GetStockPorInsumoAsync(insumo.FincaId, null, ct);
                ViewBag.StockActual = stockDict.TryGetValue(insumo.InsumoId, out var s) ? s : 0m;

                ViewBag.Movimientos = await _context.MovimientoInventarios
                    .AsNoTracking()
                    .CountAsync(m => m.InsumoId == insumo.InsumoId && m.FincaId == insumo.FincaId, ct);

                ViewBag.UltimoMov = await _context.MovimientoInventarios
                    .AsNoTracking()
                    .Where(m => m.InsumoId == insumo.InsumoId && m.FincaId == insumo.FincaId)
                    .OrderByDescending(m => m.Fecha)
                    .Select(m => m.Fecha)
                    .FirstOrDefaultAsync(ct);

                return View(insumo);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado en Insumoes.Edit (GET)");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        private void LoadSelects(Insumo? insumo = null)
        {
            ViewData["CategoriaId"] = new SelectList(
                _context.CategoriaInsumos.AsNoTracking()
                    .Select(c => new { c.CategoriaId, c.Nombre })
                    .OrderBy(c => c.Nombre),
                "CategoriaId", "Nombre",
                insumo?.CategoriaId
            );

            ViewData["UnidadId"] = new SelectList(
                _context.UnidadMedida.AsNoTracking()
                    .Select(u => new { u.UnidadId, Texto = u.Codigo + " - " + u.Nombre })
                    .OrderBy(u => u.Texto),
                "UnidadId", "Texto",
                insumo?.UnidadId
            );
        }

        // POST: Insumoes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id,
            [Bind("InsumoId,FincaId,CategoriaId,Nombre,UnidadId,StockMinimo,Activo")] Insumo dto,
            CancellationToken ct)
        {
            if (id != dto.InsumoId) return NotFound();

            try
            {
                var fincaId = GetFincaId();
                // ✅ CORRECCIÓN: Remover validaciones de navegación
                ModelState.Remove("FincaId");
                ModelState.Remove("Finca");
                ModelState.Remove("Categoria");
                ModelState.Remove("Unidad");


                if (!ModelState.IsValid)
                {
                    LoadSelects(dto);
                    return View(dto);
                }

                var insumo = await _context.Insumos
                    .FirstOrDefaultAsync(i => i.InsumoId == id && i.FincaId == fincaId, ct); // ✅ filtro

                if (insumo is null) return NotFound();

                // Validar acceso explícitamente (por si acaso)
                ValidarAcceso(insumo.FincaId);

                // Actualizar solo campos permitidos (FincaId NO se cambia)
                insumo.Nombre = dto.Nombre?.Trim() ?? insumo.Nombre;
                insumo.CategoriaId = dto.CategoriaId;
                insumo.UnidadId = dto.UnidadId;
                insumo.StockMinimo = dto.StockMinimo;
                insumo.Activo = dto.Activo;

                try
                {
                    await _context.SaveChangesAsync(ct);
                    MostrarExito("Insumo actualizado correctamente.");
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InsumoExists(id, fincaId)) return NotFound();
                    throw;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado en Insumoes.Edit (POST)");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // GET: Insumoes/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            try
            {
                var fincaId = GetFincaId();

                var insumo = await _context.Insumos
                    .Include(i => i.Categoria)
                    .Include(i => i.Finca)
                    .Include(i => i.Unidad)
                    .FirstOrDefaultAsync(m => m.InsumoId == id && m.FincaId == fincaId); // ✅ filtro

                if (insumo == null)
                    return NotFound();

                return View(insumo);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado en Insumoes.Delete (GET)");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // POST: Insumoes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var fincaId = GetFincaId();

                var insumo = await _context.Insumos
                    .FirstOrDefaultAsync(i => i.InsumoId == id && i.FincaId == fincaId); // ✅ filtro

                if (insumo != null)
                {
                    _context.Insumos.Remove(insumo);
                    await _context.SaveChangesAsync();
                    MostrarExito("Insumo eliminado correctamente.");
                }

                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado en Insumoes.Delete (POST)");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        private bool InsumoExists(long id, long fincaId)
        {
            return _context.Insumos.Any(e => e.InsumoId == id && e.FincaId == fincaId);
        }

        // Autocomplete
        // GET: /Insumos/Buscar?term=fer
        public async Task<IActionResult> Buscar(string term)
        {
            try
            {
                var fincaId = GetFincaId();
                var q = term?.Trim() ?? "";

                var data = await _context.Insumos
                    .Where(i =>
                        i.FincaId == fincaId &&                     // ✅ solo insumos de la finca
                        i.Activo &&
                        (i.Nombre.Contains(q) ||
                         i.Categoria!.Nombre.Contains(q)))
                    .OrderBy(i => i.Nombre)
                    .Take(20)
                    .Select(i => new
                    {
                        i.InsumoId,
                        Texto = i.Nombre + " (" + i.Categoria!.Nombre + ")"
                    })
                    .ToListAsync();

                return Json(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado en Insumoes.Buscar");
                return Unauthorized();
            }
        }

        // Stock por insumo/finca
        // GET: /Insumos/Stock?insumoId=123
        public async Task<IActionResult> Stock(long insumoId, long fincaId) // el parámetro fincaId se ignora
        {
            try
            {
                var usuarioFincaId = GetFincaId(); // ✅ usamos la finca del usuario

                var dict = await _inv.GetStockPorInsumoAsync(usuarioFincaId);
                var stock = dict.TryGetValue(insumoId, out var s) ? s : 0m;

                return Json(new { stock });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado en Insumoes.Stock");
                return Unauthorized();
            }
        }
    }
}