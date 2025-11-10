using System.ComponentModel.DataAnnotations;

namespace AgroTechApp.ViewModels
{
    public class EntradaVM
    {
        [Required] public long FincaId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un insumo")]
        public long InsumoId { get; set; }

        [Required, Range(0.0001, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public decimal Cantidad { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? PrecioUnitario { get; set; }

        public DateTime? Fecha { get; set; } = DateTime.UtcNow; // default hoy (UTC)

        // Lote (opcional)
        public bool UsarLote { get; set; } = false;

        [StringLength(120)]
        public string? CodigoLote { get; set; }

        public DateTime? FechaVencimiento { get; set; }

        [StringLength(300)]
        public string? Observaciones { get; set; }
    }

}
