// Services/Inventario/InventarioService.cs
using AgroTechApp.Models.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgroTechApp.Services.Inventario
{
    public class InventarioService : IInventarioService
    {
        private readonly AgroTechDbContext _db;
        private readonly ILogger<InventarioService> _logger;

        public InventarioService(AgroTechDbContext db, ILogger<InventarioService> logger)
        {
            _db = db;
            _logger = logger;
        }

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
                          .Select(g => new { g.Key, Stock = g.Sum(x => x.Cantidad) })
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
            if (cantidad <= 0)
            {
                _logger.LogWarning("Intento de registrar entrada con cantidad no positiva: {Cantidad}", cantidad);
                throw new ArgumentException("La cantidad de entrada debe ser positiva.");
            }

            // ✅ VALIDAR: Insumo pertenece a la finca
            var insumo = await _db.Insumos
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InsumoId == insumoId && i.FincaId == fincaId, ct);

            if (insumo == null)
            {
                _logger.LogWarning(
                    "Intento de registrar entrada para insumo {InsumoId} que no pertenece a finca {FincaId}",
                    insumoId, fincaId);
                throw new InvalidOperationException(
                    $"El insumo {insumoId} no pertenece a la finca {fincaId}");
            }

            // ✅ VALIDAR: Si hay lote, debe pertenecer al insumo
            if (loteId.HasValue)
            {
                var lote = await _db.InsumoLotes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.LoteId == loteId && l.InsumoId == insumoId, ct);

                if (lote == null)
                {
                    _logger.LogWarning(
                        "Intento de usar lote {LoteId} que no pertenece a insumo {InsumoId}",
                        loteId, insumoId);
                    throw new InvalidOperationException(
                        $"El lote {loteId} no existe o no pertenece al insumo {insumoId}");
                }
            }

            var tipoId = await TipoIdAsync("Compra", ct);
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

            _logger.LogInformation(
                "Entrada registrada: Finca {FincaId}, Insumo {InsumoId}, Cantidad {Cantidad}",
                fincaId, insumoId, cantidad);
        }

        public async Task RegistrarConsumoAsync(long fincaId, long insumoId, decimal cantidad,
                                                long? loteId = null, string? observacion = null, DateTime? fecha = null, CancellationToken ct = default)
        {
            if (cantidad <= 0)
            {
                _logger.LogWarning("Intento de registrar consumo con cantidad no positiva: {Cantidad}", cantidad);
                throw new ArgumentException("La cantidad de consumo debe ser positiva.");
            }

            // ✅ VALIDAR: Insumo pertenece a la finca
            var insumo = await _db.Insumos
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InsumoId == insumoId && i.FincaId == fincaId, ct);

            if (insumo == null)
            {
                _logger.LogWarning(
                    "Intento de consumo para insumo {InsumoId} que no pertenece a finca {FincaId}",
                    insumoId, fincaId);
                throw new InvalidOperationException(
                    $"El insumo {insumoId} no pertenece a la finca {fincaId}");
            }

            // ✅ VALIDAR: Si hay lote, debe pertenecer al insumo
            if (loteId.HasValue)
            {
                var lote = await _db.InsumoLotes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.LoteId == loteId && l.InsumoId == insumoId, ct);

                if (lote == null)
                {
                    _logger.LogWarning(
                        "Intento de consumo de lote {LoteId} que no pertenece a insumo {InsumoId}",
                        loteId, insumoId);
                    throw new InvalidOperationException(
                        $"El lote {loteId} no existe o no pertenece al insumo {insumoId}");
                }
            }

            // Validar stock suficiente
            var stock = await GetStockPorInsumoAsync(fincaId, loteId, ct);
            var actual = stock.TryGetValue(insumoId, out var s) ? s : 0m;

            if (actual < cantidad)
            {
                _logger.LogWarning(
                    "Stock insuficiente para consumo: Insumo {InsumoId}, Stock {Stock}, Requerido {Cantidad}",
                    insumoId, actual, cantidad);
                throw new InvalidOperationException(
                    $"Stock insuficiente: disponible {actual:N2}, requerido {cantidad:N2}.");
            }

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

            _logger.LogInformation(
                "Consumo registrado: Finca {FincaId}, Insumo {InsumoId}, Cantidad {Cantidad}",
                fincaId, insumoId, cantidad);
        }

        public async Task RegistrarAjusteAsync(long fincaId, long insumoId, decimal cantidadFirmada,
                                               long? loteId = null, string? observacion = null, DateTime? fecha = null, CancellationToken ct = default)
        {
            if (cantidadFirmada == 0)
            {
                _logger.LogWarning("Intento de ajuste con cantidad cero");
                throw new ArgumentException("El ajuste no puede ser 0.");
            }

            // ✅ VALIDAR: Insumo pertenece a la finca
            var insumo = await _db.Insumos
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InsumoId == insumoId && i.FincaId == fincaId, ct);

            if (insumo == null)
            {
                _logger.LogWarning(
                    "Intento de ajuste para insumo {InsumoId} que no pertenece a finca {FincaId}",
                    insumoId, fincaId);
                throw new InvalidOperationException(
                    $"El insumo {insumoId} no pertenece a la finca {fincaId}");
            }

            // ✅ VALIDAR: Si hay lote, debe pertenecer al insumo
            if (loteId.HasValue)
            {
                var lote = await _db.InsumoLotes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.LoteId == loteId && l.InsumoId == insumoId, ct);

                if (lote == null)
                {
                    _logger.LogWarning(
                        "Intento de ajuste de lote {LoteId} que no pertenece a insumo {InsumoId}",
                        loteId, insumoId);
                    throw new InvalidOperationException(
                        $"El lote {loteId} no existe o no pertenece al insumo {insumoId}");
                }
            }

            var tipo = cantidadFirmada > 0 ? "Ajuste+" : "Ajuste-";
            var tipoId = await TipoIdAsync(tipo, ct);

            // Si es ajuste negativo, valida stock
            if (cantidadFirmada < 0)
            {
                var stock = await GetStockPorInsumoAsync(fincaId, loteId, ct);
                var actual = stock.TryGetValue(insumoId, out var s) ? s : 0m;

                if (actual < Math.Abs(cantidadFirmada))
                {
                    _logger.LogWarning(
                        "Stock insuficiente para ajuste negativo: Insumo {InsumoId}, Stock {Stock}, Ajuste {Cantidad}",
                        insumoId, actual, cantidadFirmada);
                    throw new InvalidOperationException(
                        $"Stock insuficiente para ajuste: disponible {actual:N2}, ajuste {cantidadFirmada:N2}.");
                }
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

            _logger.LogInformation(
                "Ajuste registrado: Finca {FincaId}, Insumo {InsumoId}, Cantidad {Cantidad}",
                fincaId, insumoId, cantidadFirmada);
        }

        public async Task TransferirAsync(long insumoId, long fincaOrigen, long fincaDestino, decimal cantidadPositiva,
                                          long? loteOrigen = null, long? loteDestino = null, string? observacion = null, DateTime? fecha = null, CancellationToken ct = default)
        {
            if (cantidadPositiva <= 0)
            {
                _logger.LogWarning("Intento de transferencia con cantidad no positiva: {Cantidad}", cantidadPositiva);
                throw new ArgumentException("La cantidad a transferir debe ser positiva.");
            }

            // ✅ VALIDAR: Insumo existe en ambas fincas (o al menos en origen)
            var insumoOrigen = await _db.Insumos
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InsumoId == insumoId && i.FincaId == fincaOrigen, ct);

            if (insumoOrigen == null)
            {
                _logger.LogWarning(
                    "Intento de transferencia de insumo {InsumoId} que no pertenece a finca origen {FincaId}",
                    insumoId, fincaOrigen);
                throw new InvalidOperationException(
                    $"El insumo {insumoId} no pertenece a la finca origen {fincaOrigen}");
            }

            // ✅ VALIDAR: Lotes si se especifican
            if (loteOrigen.HasValue)
            {
                var lote = await _db.InsumoLotes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.LoteId == loteOrigen && l.InsumoId == insumoId, ct);

                if (lote == null)
                {
                    _logger.LogWarning(
                        "Lote origen {LoteId} no pertenece a insumo {InsumoId}",
                        loteOrigen, insumoId);
                    throw new InvalidOperationException(
                        $"El lote origen {loteOrigen} no pertenece al insumo {insumoId}");
                }
            }

            if (loteDestino.HasValue)
            {
                var lote = await _db.InsumoLotes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.LoteId == loteDestino && l.InsumoId == insumoId, ct);

                if (lote == null)
                {
                    _logger.LogWarning(
                        "Lote destino {LoteId} no pertenece a insumo {InsumoId}",
                        loteDestino, insumoId);
                    throw new InvalidOperationException(
                        $"El lote destino {loteDestino} no pertenece al insumo {insumoId}");
                }
            }

            var stockOrigen = await GetStockPorInsumoAsync(fincaOrigen, loteOrigen, ct);
            var actual = stockOrigen.TryGetValue(insumoId, out var s) ? s : 0m;

            if (actual < cantidadPositiva)
            {
                _logger.LogWarning(
                    "Stock insuficiente en origen para transferencia: Insumo {InsumoId}, Stock {Stock}, Requerido {Cantidad}",
                    insumoId, actual, cantidadPositiva);
                throw new InvalidOperationException(
                    $"Stock insuficiente en finca origen: disponible {actual:N2}, requerido {cantidadPositiva:N2}.");
            }

            var idMenos = await TipoIdAsync("Ajuste-", ct);
            var idMas = await TipoIdAsync("Ajuste+", ct);

            using var tx = await _db.Database.BeginTransactionAsync(ct);

            try
            {
                _db.MovimientoInventarios.Add(new MovimientoInventario
                {
                    FincaId = fincaOrigen,
                    InsumoId = insumoId,
                    LoteId = loteOrigen,
                    TipoId = idMenos,
                    Cantidad = -cantidadPositiva,
                    Fecha = fecha ?? DateTime.UtcNow,
                    Observacion = observacion ?? $"Transferencia a Finca {fincaDestino} (salida)"
                });

                _db.MovimientoInventarios.Add(new MovimientoInventario
                {
                    FincaId = fincaDestino,
                    InsumoId = insumoId,
                    LoteId = loteDestino,
                    TipoId = idMas,
                    Cantidad = +cantidadPositiva,
                    Fecha = fecha ?? DateTime.UtcNow,
                    Observacion = observacion ?? $"Transferencia desde Finca {fincaOrigen} (entrada)"
                });

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                _logger.LogInformation(
                    "Transferencia exitosa: Insumo {InsumoId}, Finca {FincaOrigen} → {FincaDestino}, Cantidad {Cantidad}",
                    insumoId, fincaOrigen, fincaDestino, cantidadPositiva);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex,
                    "Error en transferencia: Insumo {InsumoId}, {FincaOrigen} → {FincaDestino}",
                    insumoId, fincaOrigen, fincaDestino);
                throw;
            }
        }
    }
}