using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class MovimientoInventario
{
    public long MovId { get; set; }

    public long FincaId { get; set; }

    public long InsumoId { get; set; }

    public long? LoteId { get; set; }

    public int TipoId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal? CostoUnitario { get; set; }

    public DateTime Fecha { get; set; }

    public string? Observacion { get; set; }

    public virtual Finca Finca { get; set; } = null!;

    public virtual Insumo Insumo { get; set; } = null!;

    public virtual InsumoLote? Lote { get; set; }

    public virtual TipoMovimientoInventario Tipo { get; set; } = null!;
}
