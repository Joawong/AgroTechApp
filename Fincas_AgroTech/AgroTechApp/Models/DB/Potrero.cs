using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class Potrero
{
    public long PotreroId { get; set; }

    public long FincaId { get; set; }

    public string Nombre { get; set; } = null!;

    public decimal Hectareas { get; set; }

    public bool Activo { get; set; }

    public virtual Finca Finca { get; set; } = null!;

    public virtual ICollection<Gasto> Gastos { get; set; } = new List<Gasto>();

    public virtual ICollection<MovimientoAnimal> MovimientoAnimals { get; set; } = new List<MovimientoAnimal>();
}
