using AgroTechApp.Models;
using AgroTechApp.Models.DB;
using Microsoft.EntityFrameworkCore;

namespace AgroTechApp.Services
{
    /// <summary>
    /// Implementación del servicio de finanzas automáticas
    /// </summary>
    public class FinanzasService : IFinanzasService
    {
        private readonly AgroTechDbContext _context;
        private readonly ILogger<FinanzasService> _logger;

        public FinanzasService(AgroTechDbContext context, ILogger<FinanzasService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ============================================================================
        // REGISTRAR GASTOS AUTOMÁTICOS
        // ============================================================================

        public async Task<Gasto?> RegistrarGastoCompraInsumo(
            long fincaId,
            long insumoId,
            string nombreInsumo,
            decimal cantidad,
            string unidad,
            decimal costoUnitario,
            DateTime fecha,
            long movimientoId)
        {
            try
            {
                var rubroId = await ObtenerIdRubroGasto(FinanzasConstants.RubrosGasto.COMPRA_INSUMOS);
                if (!rubroId.HasValue)
                {
                    _logger.LogWarning("No se encontró el rubro 'Compra de Insumos'");
                    return null;
                }

                decimal montoTotal = cantidad * costoUnitario;

                var gasto = new Gasto
                {
                    FincaId = fincaId,
                    RubroGastoId = rubroId.Value,
                    Fecha = DateOnly.FromDateTime(fecha),
                    Monto = montoTotal,
                    Descripcion = $"Compra de {nombreInsumo} - {cantidad} {unidad}",
                    InsumoId = insumoId,
                    EsAutomatico = true,
                    OrigenModulo = FinanzasConstants.OrigenModulos.INVENTARIO,
                    ReferenciaOrigenId = movimientoId
                };

                _context.Gastos.Add(gasto);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Gasto automático creado: {GastoId} - Compra de insumo {InsumoId} por {Monto}",
                    gasto.GastoId, insumoId, montoTotal);

                return gasto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar gasto de compra de insumo");
                return null;
            }
        }

        public async Task<Gasto?> RegistrarGastoConsumoInsumo(
            long fincaId,
            long insumoId,
            string nombreInsumo,
            decimal cantidad,
            string unidad,
            DateTime fecha,
            long movimientoId,
            string? observacion = null)
        {
            try
            {
                var rubroId = await ObtenerIdRubroGasto(FinanzasConstants.RubrosGasto.ALIMENTACION);
                if (!rubroId.HasValue)
                {
                    _logger.LogWarning("No se encontró el rubro 'Alimentación'");
                    return null;
                }

                // Calcular costo promedio del insumo
                decimal costoPromedio = await CalcularCostoPromedioInsumo(insumoId);
                decimal montoTotal = cantidad * costoPromedio;

                string descripcion = $"Consumo de {nombreInsumo} - {cantidad} {unidad}";
                if (!string.IsNullOrWhiteSpace(observacion))
                {
                    descripcion += $" ({observacion})";
                }

                var gasto = new Gasto
                {
                    FincaId = fincaId,
                    RubroGastoId = rubroId.Value,
                    Fecha = DateOnly.FromDateTime(fecha),
                    Monto = montoTotal,
                    Descripcion = descripcion,
                    InsumoId = insumoId,
                    EsAutomatico = true,
                    OrigenModulo = FinanzasConstants.OrigenModulos.INVENTARIO,
                    ReferenciaOrigenId = movimientoId
                };

                _context.Gastos.Add(gasto);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Gasto automático creado: {GastoId} - Consumo de insumo {InsumoId} por {Monto}",
                    gasto.GastoId, insumoId, montoTotal);

                return gasto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar gasto de consumo de insumo");
                return null;
            }
        }

        public async Task<Gasto?> RegistrarGastoTratamiento(
            long fincaId,
            long tratamientoId,
            long? animalId,
            long? insumoId,
            string nombreInsumo,
            string tipoTratamiento,
            decimal costoTratamiento,
            DateTime fecha)
        {
            try
            {
                var rubroId = await ObtenerIdRubroGasto(FinanzasConstants.RubrosGasto.TRATAMIENTOS);
                if (!rubroId.HasValue)
                {
                    _logger.LogWarning("No se encontró el rubro 'Medicamentos y Tratamientos'");
                    return null;
                }

                string descripcion = $"Tratamiento: {tipoTratamiento}";
                if (!string.IsNullOrWhiteSpace(nombreInsumo))
                {
                    descripcion += $" - {nombreInsumo}";
                }

                var gasto = new Gasto
                {
                    FincaId = fincaId,
                    RubroGastoId = rubroId.Value,
                    Fecha = DateOnly.FromDateTime(fecha),
                    Monto = costoTratamiento,
                    Descripcion = descripcion,
                    AnimalId = animalId,
                    InsumoId = insumoId,
                    EsAutomatico = true,
                    OrigenModulo = FinanzasConstants.OrigenModulos.TRATAMIENTO,
                    ReferenciaOrigenId = tratamientoId
                };

                _context.Gastos.Add(gasto);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Gasto automático creado: {GastoId} - Tratamiento {TratamientoId} por {Monto}",
                    gasto.GastoId, tratamientoId, costoTratamiento);

                return gasto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar gasto de tratamiento");
                return null;
            }
        }

        public async Task<Gasto?> RegistrarGastoCompraAnimal(
            long fincaId,
            long animalId,
            string nombreAnimal,
            decimal costoCompra,
            DateTime fecha)
        {
            try
            {
                var rubroId = await ObtenerIdRubroGasto(FinanzasConstants.RubrosGasto.COMPRA_ANIMALES);
                if (!rubroId.HasValue)
                {
                    _logger.LogWarning("No se encontró el rubro 'Compra de Animales'");
                    return null;
                }

                var gasto = new Gasto
                {
                    FincaId = fincaId,
                    RubroGastoId = rubroId.Value,
                    Fecha = DateOnly.FromDateTime(fecha),
                    Monto = costoCompra,
                    Descripcion = $"Compra de animal: {nombreAnimal}",
                    AnimalId = animalId,
                    EsAutomatico = true,
                    OrigenModulo = FinanzasConstants.OrigenModulos.ANIMAL,
                    ReferenciaOrigenId = animalId
                };

                _context.Gastos.Add(gasto);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Gasto automático creado: {GastoId} - Compra de animal {AnimalId} por {Monto}",
                    gasto.GastoId, animalId, costoCompra);

                return gasto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar gasto de compra de animal");
                return null;
            }
        }

        // ============================================================================
        // REGISTRAR INGRESOS AUTOMÁTICOS
        // ============================================================================

        public async Task<Ingreso?> RegistrarIngresoVentaAnimal(
            long fincaId,
            long animalId,
            string nombreAnimal,
            decimal precioVenta,
            DateTime fecha,
            decimal? pesoVenta = null)
        {
            try
            {
                var rubroId = await ObtenerIdRubroIngreso(FinanzasConstants.RubrosIngreso.VENTA_ANIMALES);
                if (!rubroId.HasValue)
                {
                    _logger.LogWarning("No se encontró el rubro 'Venta de Animales'");
                    return null;
                }

                string descripcion = $"Venta de animal: {nombreAnimal}";
                if (pesoVenta.HasValue)
                {
                    descripcion += $" - {pesoVenta.Value} kg";
                }

                var ingreso = new Ingreso
                {
                    FincaId = fincaId,
                    RubroIngresoId = rubroId.Value,
                    Fecha = DateOnly.FromDateTime(fecha),
                    Monto = precioVenta,
                    Descripcion = descripcion,
                    AnimalId = animalId,
                    EsAutomatico = true,
                    OrigenModulo = FinanzasConstants.OrigenModulos.ANIMAL,
                    ReferenciaOrigenId = animalId
                };

                _context.Ingresos.Add(ingreso);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Ingreso automático creado: {IngresoId} - Venta de animal {AnimalId} por {Monto}",
                    ingreso.IngresoId, animalId, precioVenta);

                return ingreso;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar ingreso de venta de animal");
                return null;
            }
        }

        // ============================================================================
        // ELIMINACIÓN EN CASCADA
        // ============================================================================

        public async Task EliminarGastoDeMovimientoInventario(long movimientoId)
        {
            try
            {
                var gasto = await _context.Gastos
                    .FirstOrDefaultAsync(g =>
                        g.OrigenModulo == FinanzasConstants.OrigenModulos.INVENTARIO &&
                        g.ReferenciaOrigenId == movimientoId);

                if (gasto != null)
                {
                    _context.Gastos.Remove(gasto);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Gasto automático eliminado: {GastoId} asociado a movimiento {MovimientoId}",
                        gasto.GastoId, movimientoId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar gasto de movimiento de inventario");
                throw;
            }
        }

        public async Task EliminarGastoDeTratamiento(long tratamientoId)
        {
            try
            {
                var gasto = await _context.Gastos
                    .FirstOrDefaultAsync(g =>
                        g.OrigenModulo == FinanzasConstants.OrigenModulos.TRATAMIENTO &&
                        g.ReferenciaOrigenId == tratamientoId);

                if (gasto != null)
                {
                    _context.Gastos.Remove(gasto);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Gasto automático eliminado: {GastoId} asociado a tratamiento {TratamientoId}",
                        gasto.GastoId, tratamientoId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar gasto de tratamiento");
                throw;
            }
        }

        public async Task EliminarGastoDeCompraAnimal(long animalId)
        {
            try
            {
                var gasto = await _context.Gastos
                    .FirstOrDefaultAsync(g =>
                        g.OrigenModulo == FinanzasConstants.OrigenModulos.ANIMAL &&
                        g.ReferenciaOrigenId == animalId &&
                        g.RubroGasto.Nombre == FinanzasConstants.RubrosGasto.COMPRA_ANIMALES);

                if (gasto != null)
                {
                    _context.Gastos.Remove(gasto);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Gasto de compra eliminado: {GastoId} asociado a animal {AnimalId}",
                        gasto.GastoId, animalId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar gasto de compra de animal");
                throw;
            }
        }

        public async Task EliminarIngresoDeVentaAnimal(long animalId)
        {
            try
            {
                var ingreso = await _context.Ingresos
                    .FirstOrDefaultAsync(i =>
                        i.OrigenModulo == FinanzasConstants.OrigenModulos.ANIMAL &&
                        i.ReferenciaOrigenId == animalId);

                if (ingreso != null)
                {
                    _context.Ingresos.Remove(ingreso);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Ingreso de venta eliminado: {IngresoId} asociado a animal {AnimalId}",
                        ingreso.IngresoId, animalId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar ingreso de venta de animal");
                throw;
            }
        }

        // ============================================================================
        // MÉTODOS AUXILIARES
        // ============================================================================

        public async Task<decimal> CalcularCostoPromedioInsumo(long insumoId)
        {
            try
            {
                // Obtener todas las entradas de inventario del insumo con costo
                var entradas = await _context.MovimientoInventarios
                    .Where(m =>
                        m.InsumoId == insumoId &&
                        m.TipoId == FinanzasConstants.TiposMovimientoInventario.ENTRADA &&
                        m.CostoUnitario.HasValue &&
                        m.CostoUnitario.Value > 0)
                    .ToListAsync();

                if (!entradas.Any())
                {
                    _logger.LogWarning("No se encontraron entradas con costo para el insumo {InsumoId}", insumoId);
                    return 0;
                }

                // Calcular costo promedio ponderado
                decimal totalCosto = entradas.Sum(e => e.Cantidad * (e.CostoUnitario ?? 0));
                decimal totalCantidad = entradas.Sum(e => e.Cantidad);

                if (totalCantidad == 0)
                {
                    return 0;
                }

                decimal costoPromedio = totalCosto / totalCantidad;

                _logger.LogDebug(
                    "Costo promedio del insumo {InsumoId}: {CostoPromedio} ({TotalCosto}/{TotalCantidad})",
                    insumoId, costoPromedio, totalCosto, totalCantidad);

                return costoPromedio;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular costo promedio del insumo {InsumoId}", insumoId);
                return 0;
            }
        }

        public async Task<int?> ObtenerIdRubroGasto(string nombreRubro)
        {
            try
            {
                var rubro = await _context.RubroGastos
                    .FirstOrDefaultAsync(r => r.Nombre == nombreRubro);

                return rubro?.RubroGastoId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ID del rubro de gasto '{NombreRubro}'", nombreRubro);
                return null;
            }
        }

        public async Task<int?> ObtenerIdRubroIngreso(string nombreRubro)
        {
            try
            {
                var rubro = await _context.RubroIngresos
                    .FirstOrDefaultAsync(r => r.Nombre == nombreRubro);

                return rubro?.RubroIngresoId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ID del rubro de ingreso '{NombreRubro}'", nombreRubro);
                return null;
            }
        }
    }
}