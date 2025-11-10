using AgroTechApp.Models.DB;
using AgroTechApp.Services.Inventario;
using AgroTechApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AgroTechApp.Controllers
{
    public class InventarioController : Controller
    {
        private readonly AgroTechDbContext _db;
        private readonly IInventarioService _inv;
        private readonly ILogger<InventarioController> _logger;

        public InventarioController(AgroTechDbContext db, IInventarioService inv, ILogger<InventarioController> logger)
        {
            _db = db;
            _inv = inv;
            _logger = logger;
        }

        // ========= ENTRADA (GET) =========
        [HttpGet]
        public async Task<IActionResult> Entrada(long? insumoId = null, long? fincaId = null, CancellationToken ct = default)
        {
            await CargarCombosAsync(insumoId, fincaId, ct);
            return View(new EntradaVM { FincaId = fincaId ?? 0, InsumoId = insumoId ?? 0 });
        }

        // ========= ENTRADA (POST) =========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Entrada(EntradaVM vm, CancellationToken ct)
        {
            async Task Load() => await CargarCombosAsync(vm.InsumoId, vm.FincaId, ct);

            if (!ModelState.IsValid)
            {
                await Load();
                return View(vm);
            }

            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                long? loteId = await CrearOObtenerLoteOpcional(vm, ct);

                await _inv.RegistrarEntradaAsync(
                    fincaId: vm.FincaId,
                    insumoId: vm.InsumoId,
                    cantidad: vm.Cantidad,
                    costoUnitario: vm.PrecioUnitario,
                    loteId: loteId,
                    observacion: vm.Observaciones,
                    fecha: vm.Fecha ?? DateTime.UtcNow,
                    ct: ct
                );

                await tx.CommitAsync(ct);
                TempData["Ok"] = "Entrada registrada correctamente.";
                return RedirectToAction("Index", "Insumoes", new { fincaId = vm.FincaId });
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

        // ========= AUTOCOMPLETE (JSON) =========
        // GET: /Inventario/BuscarInsumos?term=fer
        [HttpGet]
        public async Task<IActionResult> BuscarInsumos(string term, long? fincaId, CancellationToken ct)
        {
            term = (term ?? string.Empty).Trim();
            var q = _db.Insumos
                .AsNoTracking()
                .Include(i => i.Categoria)
                .Where(i => i.Activo);

            if (fincaId.HasValue && fincaId.Value > 0)
                q = q.Where(i => i.FincaId == fincaId.Value);

            if (!string.IsNullOrEmpty(term))
                q = q.Where(i => i.Nombre.Contains(term) || i.Categoria!.Nombre.Contains(term));

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

        // ========= STOCK (JSON) =========
        // GET: /Inventario/Stock?insumoId=1&fincaId=2
        [HttpGet]
        public async Task<IActionResult> Stock(long insumoId, long fincaId, CancellationToken ct)
        {
            var dict = await _inv.GetStockPorInsumoAsync(fincaId, null, ct);
            var stock = dict.TryGetValue(insumoId, out var s) ? s : 0m;
            return Json(new { stock });
        }

        // ========= Helpers =========
        private async Task CargarCombosAsync(long? insumoId, long? fincaId, CancellationToken ct)
        {
            ViewData["FincaId"] = new SelectList(
                await _db.Fincas.AsNoTracking().Select(f => new { f.FincaId, f.Nombre }).ToListAsync(ct),
                "FincaId", "Nombre", fincaId
            );

            // Para iniciar, podemos cargar 10 insumos recientes (se sobre-escriben por autocomplete)
            var insumosBase = await _db.Insumos
                .AsNoTracking()
                .Include(i => i.Categoria)
                .OrderBy(i => i.Nombre)
                .Take(10)
                .Select(i => new { i.InsumoId, Texto = i.Nombre + " (" + i.Categoria!.Nombre + ")" })
                .ToListAsync(ct);

            ViewData["InsumoSelect"] = new SelectList(insumosBase, "InsumoId", "Texto", insumoId);
        }

        private async Task<long?> CrearOObtenerLoteOpcional(EntradaVM vm, CancellationToken ct)
        {
            if (!vm.UsarLote) return null;

            // Si al menos hay código o vencimiento, intentamos reutilizar
            if (!string.IsNullOrWhiteSpace(vm.CodigoLote))
            {
                var code = vm.CodigoLote.Trim();

                var existente = await _db.InsumoLotes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.InsumoId == vm.InsumoId && x.CodigoLote == code, ct);

                if (existente != null) return existente.LoteId;

                var nuevo = new InsumoLote
                {
                    InsumoId = vm.InsumoId,
                    CodigoLote = code,
                    FechaVencimiento = vm.FechaVencimiento.HasValue
                            ? DateOnly.FromDateTime(vm.FechaVencimiento.Value)
                            : (DateOnly?)null
                };
                _db.InsumoLotes.Add(nuevo);
                await _db.SaveChangesAsync(ct);
                return nuevo.LoteId;
            }

            if (vm.FechaVencimiento.HasValue)
            {
                // Reutilizar por par (InsumoId + FechaVencimiento) si NO hay código
                var fv = vm.FechaVencimiento.HasValue
                            ? DateOnly.FromDateTime(vm.FechaVencimiento.Value)
                            : (DateOnly?)null;

                var existente = await _db.InsumoLotes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.InsumoId == vm.InsumoId && x.CodigoLote == null && x.FechaVencimiento == fv, ct);

                if (existente != null) return existente.LoteId;

                var nuevo = new InsumoLote
                {
                    InsumoId = vm.InsumoId,
                    CodigoLote = null,
                    FechaVencimiento = fv
                };
                _db.InsumoLotes.Add(nuevo);
                await _db.SaveChangesAsync(ct);
                return nuevo.LoteId;
            }

            // No hay datos de lote útiles
            return null;
        }
    }
}
