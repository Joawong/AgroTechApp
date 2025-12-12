using System.ComponentModel.DataAnnotations;

namespace AgroTechApp.ViewModels
{
    /// <summary>
    /// ViewModel para registrar consumo/salida de inventario
    /// </summary>
    public class ConsumoVM
    {
        [Required]
        public long FincaId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un insumo")]
        [Display(Name = "Insumo")]
        public long InsumoId { get; set; }

        [Required]
        [Range(0.0001, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        [Display(Name = "Cantidad a consumir")]
        public decimal Cantidad { get; set; }

        [Display(Name = "Fecha de consumo")]
        public DateTime? Fecha { get; set; } = DateTime.UtcNow;

        // Opcional: especificar lote del cual consumir
        [Display(Name = "Lote específico (opcional)")]
        public long? LoteId { get; set; }

        // ========= Vinculación opcional (para trazabilidad) =========

        [Display(Name = "Animal (opcional)")]
        public long? AnimalId { get; set; }

        [Display(Name = "Lote de Animales (opcional)")]
        public long? LoteAnimalId { get; set; }

        [Display(Name = "Tratamiento (opcional)")]
        public long? TratamientoId { get; set; }

        [StringLength(300)]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        // ========= Información de ayuda (se llena desde controller) =========

        /// <summary>
        /// Stock disponible del insumo seleccionado (para mostrar en UI)
        /// </summary>
        [Display(Name = "Stock disponible")]
        public decimal? StockDisponible { get; set; }

        /// <summary>
        /// Nombre del insumo (para confirmación)
        /// </summary>
        public string? NombreInsumo { get; set; }

        /// <summary>
        /// Unidad de medida (para mostrar en UI)
        /// </summary>
        public string? Unidad { get; set; }
    }
}