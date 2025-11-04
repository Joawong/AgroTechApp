CREATE TABLE [agro].[Gasto] (
    [GastoId]      BIGINT          IDENTITY (1, 1) NOT NULL,
    [FincaId]      BIGINT          NOT NULL,
    [RubroGastoId] INT             NOT NULL,
    [Fecha]        DATE            NOT NULL,
    [Monto]        DECIMAL (18, 2) NOT NULL,
    [Descripcion]  NVARCHAR (300)  NULL,
    [AnimalId]     BIGINT          NULL,
    [PotreroId]    BIGINT          NULL,
    [InsumoId]     BIGINT          NULL,
    PRIMARY KEY CLUSTERED ([GastoId] ASC),
    FOREIGN KEY ([AnimalId]) REFERENCES [agro].[Animal] ([AnimalId]),
    FOREIGN KEY ([FincaId]) REFERENCES [agro].[Finca] ([FincaId]),
    FOREIGN KEY ([InsumoId]) REFERENCES [agro].[Insumo] ([InsumoId]),
    FOREIGN KEY ([PotreroId]) REFERENCES [agro].[Potrero] ([PotreroId]),
    FOREIGN KEY ([RubroGastoId]) REFERENCES [agro].[RubroGasto] ([RubroGastoId])
);


GO
CREATE NONCLUSTERED INDEX [IX_Gasto_Finca_Fecha]
    ON [agro].[Gasto]([FincaId] ASC, [Fecha] ASC);

