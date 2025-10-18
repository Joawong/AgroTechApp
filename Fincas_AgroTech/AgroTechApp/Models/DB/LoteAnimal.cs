using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class LoteAnimal
{
    public long LoteAnimalId { get; set; }

    public long FincaId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public virtual ICollection<Animal> Animals { get; set; } = new List<Animal>();

    public virtual Finca Finca { get; set; } = null!;

    public virtual ICollection<Tratamiento> Tratamientos { get; set; } = new List<Tratamiento>();
}
