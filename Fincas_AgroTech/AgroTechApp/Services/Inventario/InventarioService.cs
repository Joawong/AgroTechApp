// Services/Inventario/InventarioService.cs
using AgroTechApp.Models.DB;
using Microsoft.EntityFrameworkCore;

namespace AgroTechApp.Services.Inventario
{
    public class InventarioService : IInventarioService
    {
        private readonly AgroTechDbContext _db;

        public InventarioService(AgroTechDbContext db) => _db = db;

        private async Task<int> TipoIdAsync(string nombre, CancellationToken ct)
            => await _db.TipoMovimientoInventarios
                        .Where(t => t.Nombre == nombre)
                        .Select(t => t.TipoId)
                        .FirstAsync(ct);

        public async Task<Dictionary<long, decimal>> GetStockPorInsumoAsync(long? fincaId = null, long? loteId = null, CancellationToken ct = default)
        {
            var q = _db.MovimientoInventarios.AsNoTracking().AsQueryable();

            if (fincaId.HasValue) q = q.Where(m => m.FincaId == fincaId.Value);
            if (loteId.HasValue) q = q.Where(m => m.LoteId == loteId.Value);

            return await q.GroupBy(m => m.InsumoId)
                          .Select(g => new { g.Key, Stock = g.Sum(x => x.Cantidad) }) // firmado en BD
                          .ToDictionaryAsync(x => x.Key, x => x.Stock, ct);
        }

        public Task<(int activos, int porAcabarse, int agotados, int categorias)> GetKpisInsumosAsync(
            IEnumerable<Insumo> insumos, Dictionary<long, decimal> stockDict, CancellationToken ct = default)
        {
            decimal S(long id) => stockDict.TryGetValue(id, out var s) ? s : 0m;

            var activos = insumos.Count(i => i.Activo);
            var porAcabarse = insumos.Count(i => { var s = S(i.InsumoId); return s > 0 && s <= i.StockMinimo; });
            var agotados = insumos.Count(i => S(i.InsumoId) == 0);
            var categorias = insumos.Select(i => i.Categoria?.Nombre)
                                     .Where(n => !string.IsNullOrWhiteSpace(n))
                                     .Distinct()
                                     .Count();
            return Task.FromResult((activos, porAcabarse, agotados, categorias));
        }

        public async Task RegistrarEntradaAsync(long fincaId, long insumoId, decimal cantidad, decimal? costoUnitario,
                                                long? loteId = null, string? observacion = null, DateTime? fecha = null, CancellationToken ct = default)
        {
            if (cantidad <= 0) throw new ArgumentException("La cantidad de entrada debe ser positiva.");

            var tipoId = await TipoIdAsync("Compra", ct); // o "Ajuste+" para ajustes
            _db.MovimientoInventarios.Add(new MovimientoInventario
            {
                FincaId = fincaId,
                InsumoId = insumoId,
                LoteId = loteId,
                TipoId = tipoId,
                Cantidad = cantidad,
                CostoUnitario = costoUnitario,
                Fecha = fecha ?? DateTime.UtcNow,
                Observacion = observacion
            });
            await _db.SaveChangesAsync(ct);
        }

        public async Task RegistrarConsumoAsync(long fincaId, long insumoId, decimal cantidad,
                                                long? loteId = null, string? observacion = null, DateTime? fecha = null, CancellationToken ct = default)
        {
            if (cantidad <= 0) throw new ArgumentException("La cantidad de consumo debe ser positiva.");
            // Validar stock suficiente
            var stock = await GetStockPorInsumoAsync(fincaId, loteId, ct);
            var actual = stock.TryGetValue(insumoId, out var s) ? s : 0m;
            if (actual < cantidad) throw new InvalidOperationException($"Stock insuficiente: actual {actual}, requerido {cantidad}.");

            var tipoId = await TipoIdAsync("Consumo", ct);
            _db.MovimientoInventarios.Add(new MovimientoInventario
            {
                FincaId = fincaId,
                InsumoId = insumoId,
                LoteId = loteId,
                TipoId = tipoId,
                Cantidad = -cantidad,
                Fecha = fecha ?? DateTime.UtcNow,
                Observacion = observacion
            });
            await _db.SaveChangesAsync(ct);
        }

        public async Task RegistrarAjusteAsync(long fincaId, long insumoId, decimal cantidadFirmada,
                                               long? loteId = null, string? observacion = null, DateTime? fecha = null, CancellationToken ct = default)
        {
            if (cantidadFirmada == 0) throw new ArgumentException("El ajuste no puede ser 0.");

            var tipo = cantidadFirmada > 0 ? "Ajuste+" : "Ajuste-";
            var tipoId = await TipoIdAsync(tipo, ct);

            // Si es ajuste negativo, valida stock
            if (cantidadFirmada < 0)
            {
                var stock = await GetStockPorInsumoAsync(fincaId, loteId, ct);
                var actual = stock.TryGetValue(insumoId, out var s) ? s : 0m;
                if (actual < Math.Abs(cantidadFirmada))
                    throw new InvalidOperationException($"Stock insuficiente para ajuste: actual {actual}, ajuste {cantidadFirmada}.");
            }

            _db.MovimientoInventarios.Add(new MovimientoInventario
            {
                FincaId = fincaId,
                InsumoId = insumoId,
                LoteId = loteId,
                TipoId = tipoId,
                Cantidad = cantidadFirmada,
                Fecha = fecha ?? DateTime.UtcNow,
                Observacion = observacion
            });
            await _db.SaveChangesAsync(ct);
        }

        public async Task TransferirAsync(long insumoId, long fincaOrigen, long fincaDestino, decimal cantidadPositiva,
                                          long? loteOrigen = null, long? loteDestino = null, string? observacion = null, DateTime? fecha = null, CancellationToken ct = default)
        {
            if (cantidadPositiva <= 0) throw new ArgumentException("La cantidad a transferir debe ser positiva.");

            var stockOrigen = await GetStockPorInsumoAsync(fincaOrigen, loteOrigen, ct);
            var actual = stockOrigen.TryGetValue(insumoId, out var s) ? s : 0m;
            if (actual < cantidadPositiva)
                throw new InvalidOperationException($"Stock insuficiente en origen: actual {actual}, a transferir {cantidadPositiva}.");

            var idMenos = await TipoIdAsync("Ajuste-", ct); // salida técnica
            var idMas = await TipoIdAsync("Ajuste+", ct); // entrada técnica

            using var tx = await _db.Database.BeginTransactionAsync(ct);

            _db.MovimientoInventarios.Add(new MovimientoInventario
            {
                FincaId = fincaOrigen,
                InsumoId = insumoId,
                LoteId = loteOrigen,
                TipoId = idMenos,
                Cantidad = -cantidadPositiva,
                Fecha = fecha ?? DateTime.UtcNow,
                Observacion = observacion ?? "Transferencia (salida)"
            });
            _db.MovimientoInventarios.Add(new MovimientoInventario
            {
                FincaId = fincaDestino,
                InsumoId = insumoId,
                LoteId = loteDestino,
                TipoId = idMas,
                Cantidad = +cantidadPositiva,
                Fecha = fecha ?? DateTime.UtcNow,
                Observacion = observacion ?? "Transferencia (entrada)"
            });

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
    }
}

