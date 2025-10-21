using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class InsumoLote
{
    public long LoteId { get; set; }

    public long InsumoId { get; set; }

    public string? CodigoLote { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public virtual Insumo Insumo { get; set; } = null!;

    public virtual ICollection<MovimientoInventario> MovimientoInventarios { get; set; } = new List<MovimientoInventario>();

    public virtual ICollection<Tratamiento> Tratamientos { get; set; } = new List<Tratamiento>();
}
