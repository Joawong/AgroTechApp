using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class TipoMovimientoInventario
{
    public int TipoId { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<MovimientoInventario> MovimientoInventarios { get; set; } = new List<MovimientoInventario>();
}
