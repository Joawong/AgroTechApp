using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class TipoTratamiento
{
    public int TipoTratId { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Tratamiento> Tratamientos { get; set; } = new List<Tratamiento>();
}
