using AgroTechApp.Models.DB;

namespace AgroTechApp.ViewModels
{
    /// <summary>
    /// ViewModel para el historial consolidado del animal
    /// </summary>
    public class AnimalHistorialVM
    {
        // ========================================
        // INFORMACIÓN GENERAL
        // ========================================
        public Animal Animal { get; set; } = null!;
        public int DiasDeVida { get; set; }
        public string EdadFormateada { get; set; } = string.Empty;

        // ========================================
        // HISTORIAL DE PESAJES
        // ========================================
        public List<Pesaje> Pesajes { get; set; } = new();

        // Estadísticas de Peso
        public decimal? PesoActual { get; set; }
        public decimal? PesoInicial { get; set; }
        public decimal? GananciaTotal { get; set; }
        public decimal? GananciaDiariaPromedio { get; set; } // GDP

        // Mejor periodo de ganancia
        public string? MejorPeriodo { get; set; }
        public decimal? MejorGananciaDiaria { get; set; }

        // ========================================
        // HISTORIAL DE TRATAMIENTOS
        // ========================================
        public List<Tratamiento> Tratamientos { get; set; } = new();
        public int TotalTratamientos { get; set; }

        // Tratamientos por tipo
        public Dictionary<string, int> TratamientosPorTipo { get; set; } = new();

        // Próximos tratamientos (si hay periodo de retiro o repetición)
        public List<Tratamiento> TratamientosPendientes { get; set; } = new();

        // ========================================
        //  RESUMEN FINANCIERO
        // ========================================
        public decimal? CostoCompra { get; set; }
        public decimal TotalGastos { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal BalanceFinanciero { get; set; }
        public decimal? ROI { get; set; } // Solo si está vendido

        // Desglose de gastos
        public List<Gasto> Gastos { get; set; } = new();
        public List<Ingreso> Ingresos { get; set; } = new();

        // ========================================
        // INDICADORES DE RENDIMIENTO
        // ========================================
        // GDP ya está calculado arriba
        public decimal? PromedioLote { get; set; } // GDP promedio del lote
        public string? ComparativaLote { get; set; } // "Por encima/Por debajo del promedio"
        public bool TieneAlertaBajoRendimiento { get; set; }
        public string? MensajeAlerta { get; set; }

        // ========================================
        // INFORMACIÓN DEL LOTE (si aplica)
        // ========================================
        public LoteAnimal? Lote { get; set; }
        public int? AnimalesEnLote { get; set; }
        public decimal? PesoPromedioLote { get; set; }

        // ========================================
        // GENEALOGÍA
        // ========================================
        public Animal? Madre { get; set; }
        public Animal? Padre { get; set; }
        public int TotalCrias { get; set; } // Si es madre o padre
        public List<Animal> Crias { get; set; } = new();
    }
}