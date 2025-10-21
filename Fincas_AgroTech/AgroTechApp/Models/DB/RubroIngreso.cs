using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class RubroIngreso
{
    public int RubroIngresoId { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Ingreso> Ingresos { get; set; } = new List<Ingreso>();
}
