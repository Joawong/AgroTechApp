CREATE TABLE [agro].[MovimientoInventario] (
    [MovId]         BIGINT          IDENTITY (1, 1) NOT NULL,
    [FincaId]       BIGINT          NOT NULL,
    [InsumoId]      BIGINT          NOT NULL,
    [LoteId]        BIGINT          NULL,
    [TipoId]        INT             NOT NULL,
    [Cantidad]      DECIMAL (18, 3) NOT NULL,
    [CostoUnitario] DECIMAL (18, 4) NULL,
    [Fecha]         DATETIME2 (7)   DEFAULT (sysdatetime()) NOT NULL,
    [Observacion]   NVARCHAR (300)  NULL,
    PRIMARY KEY CLUSTERED ([MovId] ASC),
    CHECK ([Cantidad]<>(0)),
    FOREIGN KEY ([FincaId]) REFERENCES [agro].[Finca] ([FincaId]),
    FOREIGN KEY ([InsumoId]) REFERENCES [agro].[Insumo] ([InsumoId]),
    FOREIGN KEY ([LoteId]) REFERENCES [agro].[InsumoLote] ([LoteId]),
    FOREIGN KEY ([TipoId]) REFERENCES [agro].[TipoMovimientoInventario] ([TipoId])
);


GO
CREATE NONCLUSTERED INDEX [IX_MovInv_Insumo_Fecha]
    ON [agro].[MovimientoInventario]([InsumoId] ASC, [Fecha] ASC);

