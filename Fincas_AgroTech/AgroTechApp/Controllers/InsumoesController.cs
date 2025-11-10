using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AgroTechApp.Models.DB;
using AgroTechApp.Services.Inventario;
using Microsoft.Extensions.Logging;
using AgroTechApp.ViewModels;


namespace AgroTechApp.Controllers
{
    public class InsumoesController : Controller
    {
        private readonly AgroTechDbContext _context;
        private readonly ILogger<InsumoesController> _logger;
        private readonly IInventarioService _inv;

        public InsumoesController(AgroTechDbContext context, ILogger<InsumoesController> logger, IInventarioService inv)
        {
            _context = context; 
            _logger = logger; 
            _inv = inv;
        }

        // GET: Insumoes
        public async Task<IActionResult> Index(
            long? fincaId = null,
            int? categoriaId = null,
            string? estado = null,       // "activo" | "inactivo" | "bajo" | "agotado"
            string? q = null,            // búsqueda por nombre o categoría
            CancellationToken ct = default)
        {
            // Base query + includes para mostrar nombres
            var query = _context.Insumos
                .AsNoTracking()
                .Include(i => i.Categoria)
                .Include(i => i.Unidad)
                .Include(i => i.Finca)
                .AsQueryable();

            if (fincaId.HasValue && fincaId > 0) query = query.Where(i => i.FincaId == fincaId);
            if (categoriaId.HasValue && categoriaId > 0) query = query.Where(i => i.CategoriaId == categoriaId);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(i => i.Nombre.Contains(term) || i.Categoria!.Nombre.Contains(term));
            }

            // Trae lista provisional para calcular KPIs y status por stock
            var insumos = await query.OrderBy(i => i.Nombre).ToListAsync(ct);

            // Stock actual por insumo (por finca si viene)
            var stockDict = await _inv.GetStockPorInsumoAsync(fincaId, null, ct);

            // Filtro "estado" basado en stock/activo
            if (!string.IsNullOrEmpty(estado))
            {
                estado = estado.ToLowerInvariant();
                insumos = estado switch
                {
                    "activo" => insumos.Where(i => i.Activo).ToList(),
                    "inactivo" => insumos.Where(i => !i.Activo).ToList(),
                    "bajo" => insumos.Where(i =>
                                      (stockDict.TryGetValue(i.InsumoId, out var s) ? s : 0m) > 0
                                   && (stockDict.TryGetValue(i.InsumoId, out var s2) ? s2 : 0m) <= i.StockMinimo).ToList(),
                    "agotado" => insumos.Where(i => (stockDict.TryGetValue(i.InsumoId, out var s) ? s : 0m) == 0m).ToList(),
                    _ => insumos
                };
            }

            // KPIs (usa tu servicio)
            var (activos, porAcabarse, agotados, categorias) = await _inv.GetKpisInsumosAsync(insumos, stockDict, ct);

            ViewBag.TotalActivos = activos;
            ViewBag.PorAcabarse = porAcabarse;
            ViewBag.Agotados = agotados;
            ViewBag.Categorias = categorias;
            ViewBag.StockPorInsumo = stockDict;

            // Combos de filtros
            ViewBag.FincaId = new SelectList(await _context.Fincas.AsNoTracking().Select(f => new { f.FincaId, f.Nombre }).ToListAsync(ct), "FincaId", "Nombre", fincaId);
            ViewBag.CategoriaId = new SelectList(await _context.CategoriaInsumos.AsNoTracking().Select(c => new { c.CategoriaId, c.Nombre }).ToListAsync(ct), "CategoriaId", "Nombre", categoriaId);
            ViewBag.Estado = estado;
            ViewBag.Q = q;

            return View(insumos);
        }


        // GET: Insumoes/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var insumo = await _context.Insumos
                .Include(i => i.Categoria)
                .Include(i => i.Finca)
                .Include(i => i.Unidad)
                .FirstOrDefaultAsync(m => m.InsumoId == id);
            if (insumo == null)
            {
                return NotFound();
            }

            return View(insumo);
        }

        // GET: Insumoes/Create
        public IActionResult Create()
        {
            ViewData["FincaId"] = new SelectList(
                _context.Fincas.AsNoTracking().Select(f => new { f.FincaId, f.Nombre }),
                "FincaId", "Nombre"
            );
            ViewData["CategoriaId"] = new SelectList(
                _context.CategoriaInsumos.AsNoTracking().Select(c => new { c.CategoriaId, c.Nombre }),
                "CategoriaId", "Nombre"
            );
            ViewData["UnidadId"] = new SelectList(
                _context.UnidadMedida.AsNoTracking().Select(u => new { u.UnidadId, Texto = u.Codigo + " - " + u.Nombre }),
                "UnidadId", "Texto"
            );

            // Sugerimos fecha de hoy para la entrada inicial
            var vm = new InsumoCreateVM { FechaIngreso = DateTime.Today };
            return View(vm);
        }

        
        // POST: Insumoes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InsumoCreateVM vm, CancellationToken ct)
        {
            void LoadSelects()
            {
                ViewData["FincaId"] = new SelectList(
                    _context.Fincas.AsNoTracking().Select(f => new { f.FincaId, f.Nombre }),
                    "FincaId", "Nombre", vm.FincaId);
                ViewData["CategoriaId"] = new SelectList(
                    _context.CategoriaInsumos.AsNoTracking().Select(c => new { c.CategoriaId, c.Nombre }),
                    "CategoriaId", "Nombre", vm.CategoriaId);
                ViewData["UnidadId"] = new SelectList(
                    _context.UnidadMedida.AsNoTracking().Select(u => new { u.UnidadId, Texto = u.Codigo + " - " + u.Nombre }),
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
                    FincaId = vm.FincaId,
                    CategoriaId = vm.CategoriaId,
                    UnidadId = vm.UnidadId,
                    StockMinimo = vm.StockMinimo,
                    Activo = vm.Activo
                };
                _context.Insumos.Add(insumo);
                await _context.SaveChangesAsync(ct);

                // 2) (Opcional) lote si hay vencimiento/código
                long? loteId = null;
                if (!string.IsNullOrWhiteSpace(vm.CodigoLote) || vm.FechaVencimiento.HasValue)
                {
                    var lote = new InsumoLote
                    {
                        InsumoId = insumo.InsumoId,
                        CodigoLote = string.IsNullOrWhiteSpace(vm.CodigoLote) ? null : vm.CodigoLote!.Trim(),
                        // CONVERSIÓN DateTime? -> DateOnly?
                        FechaVencimiento = vm.FechaVencimiento.HasValue
                            ? DateOnly.FromDateTime(vm.FechaVencimiento.Value)
                            : (DateOnly?)null
                    };
                    _context.InsumoLotes.Add(lote);
                    await _context.SaveChangesAsync(ct);
                    loteId = lote.LoteId;
                }

                // 3) (Opcional) movimiento de entrada inicial
                if (vm.RegistrarEntradaInicial && vm.Cantidad is > 0 && vm.FechaIngreso.HasValue)
                {
                    await _inv.RegistrarEntradaAsync(
                        fincaId: vm.FincaId,
                        insumoId: insumo.InsumoId,
                        cantidad: vm.Cantidad.Value,
                        costoUnitario: vm.PrecioUnitario,
                        loteId: loteId,
                        observacion: vm.Observaciones,
                        fecha: vm.FechaIngreso.Value,
                        ct: ct // 👈 NOMBRE explícito del parámetro
                    );
                }

                await tx.CommitAsync(ct);
                TempData["Ok"] = "Insumo creado correctamente.";
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



        // GET: Insumoes/Edit/5
        public async Task<IActionResult> Edit(long? id, CancellationToken ct)
        {
            if (id is null) return NotFound();

            var insumo = await _context.Insumos
                .Include(i => i.Finca)
                .Include(i => i.Categoria)
                .Include(i => i.Unidad)
                .FirstOrDefaultAsync(i => i.InsumoId == id, ct);

            if (insumo is null) return NotFound();

            // Combos (solo categoría y unidad; finca no editable)
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

            // Nombres actuales para mostrar
            ViewBag.FincaNombre = insumo.Finca?.Nombre ?? $"Finca #{insumo.FincaId}";
            ViewBag.CategoriaActual = insumo.Categoria?.Nombre ?? $"#{insumo.CategoriaId}";
            ViewBag.UnidadActual = insumo.Unidad is null ? $"#{insumo.UnidadId}" : $"{insumo.Unidad.Codigo} - {insumo.Unidad.Nombre}";

            // Métricas de inventario para el sidebar
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


        private void LoadSelects(AgroTechApp.Models.DB.Insumo? insumo = null)
        {
            ViewData["FincaId"] = new SelectList(
                _context.Fincas.AsNoTracking()
                    .Select(f => new { f.FincaId, f.Nombre })
                    .OrderBy(f => f.Nombre),
                "FincaId", "Nombre",
                insumo?.FincaId
            );

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
        public async Task<IActionResult> Edit(long id, [Bind("InsumoId,FincaId,CategoriaId,Nombre,UnidadId,StockMinimo,Activo")] Insumo dto, CancellationToken ct)
        {
            if (id != dto.InsumoId) return NotFound();

            if (!ModelState.IsValid)
            {
                LoadSelects(dto);
                return View(dto);
            }

            var insumo = await _context.Insumos.FirstOrDefaultAsync(i => i.InsumoId == id, ct);
            if (insumo is null) return NotFound();

            // Actualiza SOLO campos permitidos
            insumo.Nombre = dto.Nombre?.Trim() ?? insumo.Nombre;
            insumo.FincaId = dto.FincaId;
            insumo.CategoriaId = dto.CategoriaId;
            insumo.UnidadId = dto.UnidadId;
            insumo.StockMinimo = dto.StockMinimo;
            insumo.Activo = dto.Activo;

            try
            {
                await _context.SaveChangesAsync(ct);
                TempData["Ok"] = "Insumo actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Insumos.Any(e => e.InsumoId == id)) return NotFound();
                throw;
            }
        }


        // GET: Insumoes/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var insumo = await _context.Insumos
                .Include(i => i.Categoria)
                .Include(i => i.Finca)
                .Include(i => i.Unidad)
                .FirstOrDefaultAsync(m => m.InsumoId == id);
            if (insumo == null)
            {
                return NotFound();
            }

            return View(insumo);
        }

        // POST: Insumoes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var insumo = await _context.Insumos.FindAsync(id);
            if (insumo != null)
            {
                _context.Insumos.Remove(insumo);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool InsumoExists(long id)
        {
            return _context.Insumos.Any(e => e.InsumoId == id);
        }


        // Autocomplete
        // GET: /Insumos/Buscar?term=fer
        public async Task<IActionResult> Buscar(string term)
        {
            var q = term?.Trim() ?? "";
            var data = await _context.Insumos
                .Where(i => i.Activo && (i.Nombre.Contains(q) || i.Categoria.Nombre.Contains(q)))
                .OrderBy(i => i.Nombre)
                .Take(20)
                .Select(i => new { i.InsumoId, Texto = i.Nombre + " (" + i.Categoria.Nombre + ")" })
                .ToListAsync();
            return Json(data);
        }

        // Stock por insumo/finca
        // GET: /Insumos/Stock?insumoId=123&fincaId=1
        public async Task<IActionResult> Stock(long insumoId, long fincaId)
        {
            var dict = await _inv.GetStockPorInsumoAsync(fincaId);
            var stock = dict.TryGetValue(insumoId, out var s) ? s : 0m;
            return Json(new { stock });
        }
    }
}
