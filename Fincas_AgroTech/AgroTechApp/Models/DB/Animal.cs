using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class Animal
{
    public long AnimalId { get; set; }

    public long FincaId { get; set; }

    public string Arete { get; set; } = null!;

    public string? Nombre { get; set; }

    public string Sexo { get; set; } = null!;

    public int? RazaId { get; set; }

    public DateOnly? FechaNacimiento { get; set; }

    public decimal? PesoNacimiento { get; set; }

    public string Estado { get; set; } = null!;

    public long? MadreId { get; set; }

    public long? PadreId { get; set; }

    public long? LoteAnimalId { get; set; }

    public virtual Finca Finca { get; set; } = null!;

    public virtual ICollection<Gasto> Gastos { get; set; } = new List<Gasto>();

    public virtual ICollection<Ingreso> Ingresos { get; set; } = new List<Ingreso>();

    public virtual ICollection<Animal> InverseMadre { get; set; } = new List<Animal>();

    public virtual ICollection<Animal> InversePadre { get; set; } = new List<Animal>();

    public virtual LoteAnimal? LoteAnimal { get; set; }

    public virtual Animal? Madre { get; set; }

    public virtual ICollection<Mortalidad> Mortalidads { get; set; } = new List<Mortalidad>();

    public virtual ICollection<MovimientoAnimal> MovimientoAnimals { get; set; } = new List<MovimientoAnimal>();

    public virtual Animal? Padre { get; set; }

    public virtual ICollection<Pesaje> Pesajes { get; set; } = new List<Pesaje>();

    public virtual Raza? Raza { get; set; }

    public virtual ICollection<Tratamiento> Tratamientos { get; set; } = new List<Tratamiento>();
}
