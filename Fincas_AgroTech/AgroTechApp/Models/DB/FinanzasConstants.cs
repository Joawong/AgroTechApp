namespace AgroTechApp.Models
{
    /// <summary>
    /// Constantes para el sistema de finanzas automáticas
    /// </summary>
    public static class FinanzasConstants
    {
        // ============================================================================
        // NOMBRES DE RUBROS DE GASTO
        // ============================================================================
        public static class RubrosGasto
        {
            public const string COMPRA_INSUMOS = "Compra de Insumos";
            public const string ALIMENTACION = "Alimentación";
            public const string COMPRA_ANIMALES = "Compra de Animales";
            public const string MANO_OBRA = "Mano de Obra";
            public const string MANTENIMIENTO = "Mantenimiento";
            public const string TRANSPORTE = "Transporte";
            public const string SERVICIOS_VETERINARIOS = "Servicios Veterinarios";
            public const string OTROS_GASTOS = "Otros Gastos";
            public const string PERDIDAS_MORTALIDAD = "Pérdidas por Mortalidad";
            public const string CONSUMO_INSUMOS = "Consumo de Insumos";
            public const string TRATAMIENTOS = "Tratamientos Veterinarios";
        }

        // ============================================================================
        // NOMBRES DE RUBROS DE INGRESO
        // ============================================================================
        public static class RubrosIngreso
        {
            public const string VENTA_ANIMALES = "Venta de Animales";
            public const string VENTA_LECHE = "Venta de Leche";
            public const string VENTA_CARNE = "Venta de Carne";
            public const string SERVICIOS = "Servicios";
            public const string SUBSIDIOS = "Subsidios";
            public const string OTROS_INGRESOS = "Otros Ingresos";
        }

        // ============================================================================
        // MÓDULOS DE ORIGEN
        // ============================================================================
        public static class OrigenModulos
        {
            public const string INVENTARIO = "Inventario";
            public const string TRATAMIENTO = "Tratamiento";
            public const string ANIMAL = "Animal";
            public const string MANUAL = "Manual";
            public const string MORTALIDAD = "Mortalidad";
        }

        // ============================================================================
        // TIPOS DE MOVIMIENTO INVENTARIO
        // ============================================================================
        public static class TiposMovimientoInventario
        {
            public const int ENTRADA = 1;
            public const int CONSUMO = 2;
            public const int AJUSTE = 3;
            public const int TRANSFERENCIA_SALIDA = 4;
            public const int TRANSFERENCIA_ENTRADA = 5;
        }
    }
}