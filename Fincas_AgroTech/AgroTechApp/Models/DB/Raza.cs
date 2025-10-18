using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class Raza
{
    public int RazaId { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Animal> Animals { get; set; } = new List<Animal>();
}
