using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class Rol
{
    public int RolId { get; set; }

    public string Nombre { get; set; } = null!;

    public DateTime? FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public Guid? UsuarioCrea { get; set; }

    public Guid? UsuarioModifica { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
