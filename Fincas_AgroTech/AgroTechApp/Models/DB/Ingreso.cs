using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class Ingreso
{
    public long IngresoId { get; set; }

    public long FincaId { get; set; }

    public int RubroIngresoId { get; set; }

    public DateOnly Fecha { get; set; }

    public decimal Monto { get; set; }

    public string? Descripcion { get; set; }

    public long? AnimalId { get; set; }

    // Sistema de Flags
    public bool EsAutomatico { get; set; }

    public string? OrigenModulo { get; set; }

    public long? ReferenciaOrigenId { get; set; }

    // Navegación
    public virtual Animal? Animal { get; set; }

    public virtual Finca Finca { get; set; } = null!;

    public virtual RubroIngreso RubroIngreso { get; set; } = null!;
}