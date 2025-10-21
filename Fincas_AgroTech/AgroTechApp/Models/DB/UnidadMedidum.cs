using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class UnidadMedidum
{
    public int UnidadId { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Insumo> Insumos { get; set; } = new List<Insumo>();
}
