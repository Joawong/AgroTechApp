CREATE TABLE [agro].[InsumoLote] (
    [LoteId]           BIGINT         IDENTITY (1, 1) NOT NULL,
    [InsumoId]         BIGINT         NOT NULL,
    [CodigoLote]       NVARCHAR (120) NULL,
    [FechaVencimiento] DATE           NULL,
    PRIMARY KEY CLUSTERED ([LoteId] ASC),
    FOREIGN KEY ([InsumoId]) REFERENCES [agro].[Insumo] ([InsumoId]),
    UNIQUE NONCLUSTERED ([InsumoId] ASC, [CodigoLote] ASC)
);

