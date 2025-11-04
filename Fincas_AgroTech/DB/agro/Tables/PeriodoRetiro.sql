CREATE TABLE [agro].[PeriodoRetiro] (
    [RetiroId]      BIGINT        IDENTITY (1, 1) NOT NULL,
    [TratamientoId] BIGINT        NOT NULL,
    [FechaDesde]    DATE          NOT NULL,
    [FechaHasta]    DATE          NOT NULL,
    [Producto]      NVARCHAR (40) NOT NULL,
    PRIMARY KEY CLUSTERED ([RetiroId] ASC),
    CHECK ([Producto]=N'Carne' OR [Producto]=N'Leche'),
    FOREIGN KEY ([TratamientoId]) REFERENCES [agro].[Tratamiento] ([TratamientoId])
);

