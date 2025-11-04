using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class Insumo
{
    public long InsumoId { get; set; }

    public long FincaId { get; set; }

    public int CategoriaId { get; set; }

    public string Nombre { get; set; } = null!;

    public int UnidadId { get; set; }

    public decimal StockMinimo { get; set; }

    public bool Activo { get; set; }

    public virtual CategoriaInsumo Categoria { get; set; } = null!;

    public virtual Finca Finca { get; set; } = null!;

    public virtual ICollection<Gasto> Gastos { get; set; } = new List<Gasto>();

    public virtual ICollection<InsumoLote> InsumoLotes { get; set; } = new List<InsumoLote>();

    public virtual ICollection<MovimientoInventario> MovimientoInventarios { get; set; } = new List<MovimientoInventario>();

    public virtual ICollection<Tratamiento> Tratamientos { get; set; } = new List<Tratamiento>();

    public virtual UnidadMedidum Unidad { get; set; } = null!;
}
