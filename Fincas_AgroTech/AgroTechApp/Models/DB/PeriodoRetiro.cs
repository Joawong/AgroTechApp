using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class PeriodoRetiro
{
    public long RetiroId { get; set; }

    public long TratamientoId { get; set; }

    public DateOnly FechaDesde { get; set; }

    public DateOnly FechaHasta { get; set; }

    public string Producto { get; set; } = null!;

    public virtual Tratamiento Tratamiento { get; set; } = null!;
}
