using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class Gasto
{
    public long GastoId { get; set; }

    public long FincaId { get; set; }

    public int RubroGastoId { get; set; }

    public DateOnly Fecha { get; set; }

    public decimal Monto { get; set; }

    public string? Descripcion { get; set; }

    public long? AnimalId { get; set; }

    public long? PotreroId { get; set; }

    public long? InsumoId { get; set; }

    public virtual Animal? Animal { get; set; }

    public virtual Finca Finca { get; set; } = null!;

    public virtual Insumo? Insumo { get; set; }

    public virtual Potrero? Potrero { get; set; }

    public virtual RubroGasto RubroGasto { get; set; } = null!;
}
