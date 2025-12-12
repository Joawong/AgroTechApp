using AgroTechApp.Models.DB;

namespace AgroTechApp.ViewModels.AnimalVM
{
    public class AnimalDetailsViewModel
    {
        // El animal principal
        public Animal Animal { get; set; } = null!;

        // Información calculada
        public int EdadMeses { get; set; }
        public bool PuedeCalcularRendimiento { get; set; }

        // Comparativas
        public ComparativaMetrica? ComparativaGDP { get; set; }
        public ComparativaMetrica? ComparativaPeso { get; set; }

        // Alertas
        public List<AlertaRendimiento> Alertas { get; set; } = new List<AlertaRendimiento>();

        // Resumen de pesajes
        public ResumenPesajes? ResumenPesajes { get; set; }
    }

    // Clase para comparativas (GDP, Peso, etc.)
    public class ComparativaMetrica
    {
        public decimal ValorAnimal { get; set; }
        public decimal ValorReferencia { get; set; }
        public decimal Diferencia { get; set; } // Porcentaje de diferencia
        public bool EsMejor { get; set; } // True si el animal está mejor que la referencia
        public bool EnRango { get; set; } // True si está en rango normal (±10%)
        public string Etiqueta { get; set; } = string.Empty;
    }

    // Clase para alertas
    public class AlertaRendimiento
    {
        public string Tipo { get; set; } = "warning"; // danger, warning, info
        public string Icono { get; set; } = "exclamation-triangle-fill";
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }

    // Clase para resumen de pesajes
    public class ResumenPesajes
    {
        public int TotalPesajes { get; set; }
        public decimal PesoInicial { get; set; }
        public decimal PesoActual { get; set; }
        public decimal GananciaTotal { get; set; }
        public DateTime FechaPrimerPesaje { get; set; }
        public DateTime FechaUltimoPesaje { get; set; }
        public int DiasEntrePesajes { get; set; }
    }
}