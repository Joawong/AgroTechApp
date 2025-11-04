using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AgroTechApp.Models.DB;

public partial class Pesaje
{
    public long PesajeId { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un animal.")]
    public long? AnimalId { get; set; }   // <- nullable para el binder

    [Required]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime Fecha { get; set; }

    [Required]
    [Range(1, 2000, ErrorMessage = "El peso debe estar entre 1 y 2000 kg.")]
    public decimal PesoKg { get; set; }

    public string? Observacion { get; set; }

    public virtual Animal Animal { get; set; } = null!;

}
