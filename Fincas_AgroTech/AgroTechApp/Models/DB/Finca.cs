using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class Finca
{
    public long FincaId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Ubicacion { get; set; }

    public bool Activa { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<Animal> Animals { get; set; } = new List<Animal>();

    public virtual ICollection<Gasto> Gastos { get; set; } = new List<Gasto>();

    public virtual ICollection<Ingreso> Ingresos { get; set; } = new List<Ingreso>();

    public virtual ICollection<Insumo> Insumos { get; set; } = new List<Insumo>();

    public virtual ICollection<LoteAnimal> LoteAnimals { get; set; } = new List<LoteAnimal>();

    public virtual ICollection<MovimientoInventario> MovimientoInventarios { get; set; } = new List<MovimientoInventario>();

    public virtual ICollection<Potrero> Potreros { get; set; } = new List<Potrero>();

    public virtual ICollection<Tratamiento> Tratamientos { get; set; } = new List<Tratamiento>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
