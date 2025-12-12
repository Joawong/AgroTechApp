using System.ComponentModel.DataAnnotations;

namespace AgroTechApp.ViewModels.Insumo
{
    public class InsumoCreateVM : IValidatableObject
    {
        // ====== Campos del catálogo (agro.Insumo) ======
        [Required, StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        public long FincaId { get; set; }

        [Required]
        public int CategoriaId { get; set; }

        [Required]
        public int UnidadId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal StockMinimo { get; set; } = 0;

        public bool Activo { get; set; } = true;

        // ====== Entrada inicial (opcional, NO existe en agro.Insumo) ======
        public bool RegistrarEntradaInicial { get; set; } = true;

        [Range(0.0001, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public decimal? Cantidad { get; set; }    // requerido si RegistrarEntradaInicial = true

        [Range(0, double.MaxValue)]
        public decimal? PrecioUnitario { get; set; } // opcional

        public DateTime? FechaIngreso { get; set; }  // requerido si RegistrarEntradaInicial = true

        public DateTime? FechaVencimiento { get; set; } // opcional (si hay lote perecedero)
        [StringLength(120)]
        public string? CodigoLote { get; set; } // opcional

        [StringLength(300)]
        public string? Observaciones { get; set; } // opcional (comentario del movimiento)

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (RegistrarEntradaInicial)
            {
                if (Cantidad is null || Cantidad <= 0)
                    yield return new ValidationResult("Debe indicar una cantidad > 0.", new[] { nameof(Cantidad) });

                if (FechaIngreso is null)
                    yield return new ValidationResult("Debe indicar la fecha de ingreso.", new[] { nameof(FechaIngreso) });

                if (FechaVencimiento.HasValue && FechaIngreso.HasValue && FechaVencimiento < FechaIngreso)
                    yield return new ValidationResult("La fecha de vencimiento no puede ser anterior a la fecha de ingreso.", new[] { nameof(FechaVencimiento) });
            }
        }
    }
}
