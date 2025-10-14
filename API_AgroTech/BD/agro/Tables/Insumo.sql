CREATE TABLE [agro].[Insumo] (
    [InsumoId]    BIGINT          IDENTITY (1, 1) NOT NULL,
    [FincaId]     BIGINT          NOT NULL,
    [CategoriaId] INT             NOT NULL,
    [Nombre]      NVARCHAR (200)  NOT NULL,
    [UnidadId]    INT             NOT NULL,
    [StockMinimo] DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
    [Activo]      BIT             DEFAULT ((1)) NOT NULL,
    PRIMARY KEY CLUSTERED ([InsumoId] ASC),
    FOREIGN KEY ([CategoriaId]) REFERENCES [agro].[CategoriaInsumo] ([CategoriaId]),
    FOREIGN KEY ([FincaId]) REFERENCES [agro].[Finca] ([FincaId]),
    FOREIGN KEY ([UnidadId]) REFERENCES [agro].[UnidadMedida] ([UnidadId]),
    UNIQUE NONCLUSTERED ([FincaId] ASC, [Nombre] ASC)
);

