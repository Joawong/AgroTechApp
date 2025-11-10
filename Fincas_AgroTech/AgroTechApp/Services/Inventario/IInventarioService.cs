using AgroTechApp.Models.DB;

namespace AgroTechApp.Services.Inventario
{
    public interface IInventarioService
    {
        Task<Dictionary<long, decimal>> GetStockPorInsumoAsync(
            long? fincaId = null, long? loteId = null, CancellationToken ct = default);

        Task<(int activos, int porAcabarse, int agotados, int categorias)> GetKpisInsumosAsync(
            IEnumerable<Insumo> insumos, Dictionary<long, decimal> stockDict, CancellationToken ct = default);

        Task RegistrarEntradaAsync(long fincaId, long insumoId, decimal cantidad, decimal? costoUnitario,
                                   long? loteId = null, string? observacion = null, DateTime? fecha = null, CancellationToken ct = default);

        Task RegistrarConsumoAsync(long fincaId, long insumoId, decimal cantidad,
                                   long? loteId = null, string? observacion = null, DateTime? fecha = null, CancellationToken ct = default);

        Task RegistrarAjusteAsync(long fincaId, long insumoId, decimal cantidadFirmada,
                                  long? loteId = null, string? observacion = null, DateTime? fecha = null, CancellationToken ct = default);

        Task TransferirAsync(long insumoId, long fincaOrigen, long fincaDestino, decimal cantidadPositiva,
                             long? loteOrigen = null, long? loteDestino = null, string? observacion = null, DateTime? fecha = null, CancellationToken ct = default);
    }
}
