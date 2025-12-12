using AgroTechApp.Models.DB;

namespace AgroTechApp.Services
{
    /// <summary>
    /// Servicio centralizado para gestión automática de gastos e ingresos
    /// </summary>
    public interface IFinanzasService
    {
        // ============================================================================
        // METODOS PARA GASTOS AUTOMÁTICOS
        // ============================================================================

        /// <summary>
        /// Registra un gasto automático por compra de insumo (Entrada de inventario)
        /// </summary>
        /// <param name="fincaId">ID de la finca</param>
        /// <param name="insumoId">ID del insumo comprado</param>
        /// <param name="nombreInsumo">Nombre del insumo</param>
        /// <param name="cantidad">Cantidad comprada</param>
        /// <param name="unidad">Unidad de medida</param>
        /// <param name="costoUnitario">Costo por unidad</param>
        /// <param name="fecha">Fecha de la compra</param>
        /// <param name="movimientoId">ID del MovimientoInventario que genera el gasto</param>
        Task<Gasto?> RegistrarGastoCompraInsumo(
            long fincaId,
            long insumoId,
            string nombreInsumo,
            decimal cantidad,
            string unidad,
            decimal costoUnitario,
            DateTime fecha,
            long movimientoId);

        /// <summary>
        /// Registra un gasto automático por consumo de insumo
        /// </summary>
        /// <param name="fincaId">ID de la finca</param>
        /// <param name="insumoId">ID del insumo consumido</param>
        /// <param name="nombreInsumo">Nombre del insumo</param>
        /// <param name="cantidad">Cantidad consumida</param>
        /// <param name="unidad">Unidad de medida</param>
        /// <param name="fecha">Fecha del consumo</param>
        /// <param name="movimientoId">ID del MovimientoInventario que genera el gasto</param>
        /// <param name="observacion">Observación adicional (opcional)</param>
        Task<Gasto?> RegistrarGastoConsumoInsumo(
            long fincaId,
            long insumoId,
            string nombreInsumo,
            decimal cantidad,
            string unidad,
            DateTime fecha,
            long movimientoId,
            string? observacion = null);

        /// <summary>
        /// Registra un gasto automático por aplicación de tratamiento
        /// </summary>
        /// <param name="fincaId">ID de la finca</param>
        /// <param name="tratamientoId">ID del tratamiento aplicado</param>
        /// <param name="animalId">ID del animal tratado (opcional)</param>
        /// <param name="insumoId">ID del insumo usado en el tratamiento (opcional)</param>
        /// <param name="nombreInsumo">Nombre del insumo usado</param>
        /// <param name="tipoTratamiento">Nombre del tipo de tratamiento</param>
        /// <param name="costoTratamiento">Costo total del tratamiento</param>
        /// <param name="fecha">Fecha del tratamiento</param>
        Task<Gasto?> RegistrarGastoTratamiento(
            long fincaId,
            long tratamientoId,
            long? animalId,
            long? insumoId,
            string nombreInsumo,
            string tipoTratamiento,
            decimal costoTratamiento,
            DateTime fecha);

        /// <summary>
        /// Registra un gasto automático por compra de animal
        /// </summary>
        /// <param name="fincaId">ID de la finca</param>
        /// <param name="animalId">ID del animal comprado</param>
        /// <param name="nombreAnimal">Nombre o identificación del animal</param>
        /// <param name="costoCompra">Costo de compra del animal</param>
        /// <param name="fecha">Fecha de la compra</param>
        Task<Gasto?> RegistrarGastoCompraAnimal(
            long fincaId,
            long animalId,
            string nombreAnimal,
            decimal costoCompra,
            DateTime fecha);

        // ============================================================================
        // MÉTODOS PARA INGRESOS AUTOMÁTICOS
        // ============================================================================

        /// <summary>
        /// Registra un ingreso automático por venta de animal
        /// </summary>
        /// <param name="fincaId">ID de la finca</param>
        /// <param name="animalId">ID del animal vendido</param>
        /// <param name="nombreAnimal">Nombre o identificación del animal</param>
        /// <param name="precioVenta">Precio de venta del animal</param>
        /// <param name="fecha">Fecha de la venta</param>
        /// <param name="pesoVenta">Peso del animal al momento de la venta (opcional)</param>
        Task<Ingreso?> RegistrarIngresoVentaAnimal(
            long fincaId,
            long animalId,
            string nombreAnimal,
            decimal precioVenta,
            DateTime fecha,
            decimal? pesoVenta = null);

        // ============================================================================
        // MÉTODOS PARA ELIMINACIÓN EN CASCADA
        // ============================================================================

        /// <summary>
        /// Elimina el gasto asociado a un movimiento de inventario
        /// </summary>
        /// <param name="movimientoId">ID del MovimientoInventario</param>
        Task EliminarGastoDeMovimientoInventario(long movimientoId);

        /// <summary>
        /// Elimina el gasto asociado a un tratamiento
        /// </summary>
        /// <param name="tratamientoId">ID del Tratamiento</param>
        Task EliminarGastoDeTratamiento(long tratamientoId);

        /// <summary>
        /// Elimina el gasto asociado a la compra de un animal
        /// </summary>
        /// <param name="animalId">ID del Animal</param>
        Task EliminarGastoDeCompraAnimal(long animalId);

        /// <summary>
        /// Elimina el ingreso asociado a la venta de un animal
        /// </summary>
        /// <param name="animalId">ID del Animal</param>
        Task EliminarIngresoDeVentaAnimal(long animalId);

        // ============================================================================
        // METODOS AUXILIARES
        // ============================================================================

        /// <summary>
        /// Calcula el costo promedio ponderado de un insumo
        /// </summary>
        /// <param name="insumoId">ID del insumo</param>
        /// <returns>Costo promedio por unidad</returns>
        Task<decimal> CalcularCostoPromedioInsumo(long insumoId);

        /// <summary>
        /// Obtiene el ID del rubro de gasto por nombre
        /// </summary>
        /// <param name="nombreRubro">Nombre del rubro</param>
        /// <returns>ID del rubro o null si no existe</returns>
        Task<int?> ObtenerIdRubroGasto(string nombreRubro);

        /// <summary>
        /// Obtiene el ID del rubro de ingreso por nombre
        /// </summary>
        /// <param name="nombreRubro">Nombre del rubro</param>
        /// <returns>ID del rubro o null si no existe</returns>
        Task<int?> ObtenerIdRubroIngreso(string nombreRubro);
    }
}