using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class RubroGasto
{
    public int RubroGastoId { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Gasto> Gastos { get; set; } = new List<Gasto>();
}
