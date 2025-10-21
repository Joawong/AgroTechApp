using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class MovimientoAnimal
{
    public long MovAnimalId { get; set; }

    public long AnimalId { get; set; }

    public long? PotreroId { get; set; }

    public DateTime FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }

    public string? Observacion { get; set; }

    public virtual Animal Animal { get; set; } = null!;

    public virtual Potrero? Potrero { get; set; }
}
