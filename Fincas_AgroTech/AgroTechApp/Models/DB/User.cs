using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class User
{
    public long UserId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Email { get; set; } = null!;

    public byte[] HashPassword { get; set; } = null!;

    public int RolId { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Rol Rol { get; set; } = null!;

    public virtual ICollection<Finca> Fincas { get; set; } = new List<Finca>();
}
