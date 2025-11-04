using System;
using System.Collections.Generic;

namespace AgroTechApp.Models.DB;

public partial class Tratamiento
{
    public long TratamientoId { get; set; }

    public long FincaId { get; set; }

    public int TipoTratId { get; set; }

    public long? AnimalId { get; set; }

    public long? LoteAnimalId { get; set; }

    public DateTime Fecha { get; set; }

    public long? InsumoId { get; set; }

    public long? LoteId { get; set; }

    public string? Dosis { get; set; }

    public string? Via { get; set; }

    public string? Responsable { get; set; }

    public string? Observacion { get; set; }

    public virtual Animal? Animal { get; set; }

    public virtual Finca Finca { get; set; } = null!;

    public virtual Insumo? Insumo { get; set; }

    public virtual InsumoLote? Lote { get; set; }

    public virtual LoteAnimal? LoteAnimal { get; set; }

    public virtual ICollection<PeriodoRetiro> PeriodoRetiros { get; set; } = new List<PeriodoRetiro>();

    public virtual TipoTratamiento TipoTrat { get; set; } = null!;
}
