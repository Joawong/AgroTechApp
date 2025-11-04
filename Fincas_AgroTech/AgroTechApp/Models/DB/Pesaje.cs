using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class Pesaje
{
    public long PesajeId { get; set; }

    public long AnimalId { get; set; }

    public DateTime Fecha { get; set; }

    public decimal PesoKg { get; set; }

    public string? Observacion { get; set; }

    public virtual Animal Animal { get; set; } = null!;
    
}
