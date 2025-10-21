using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class Mortalidad
{
    public long MortalidadId { get; set; }

    public long AnimalId { get; set; }

    public DateTime Fecha { get; set; }

    public string? Causa { get; set; }

    public string? Observacion { get; set; }

    public virtual Animal Animal { get; set; } = null!;
}
