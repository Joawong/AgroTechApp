using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class CategoriaInsumo
{
    public int CategoriaId { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Insumo> Insumos { get; set; } = new List<Insumo>();
}
