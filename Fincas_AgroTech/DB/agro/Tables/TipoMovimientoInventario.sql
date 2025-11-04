CREATE TABLE [agro].[TipoMovimientoInventario] (
    [TipoId] INT           IDENTITY (1, 1) NOT NULL,
    [Nombre] NVARCHAR (80) NOT NULL,
    PRIMARY KEY CLUSTERED ([TipoId] ASC),
    UNIQUE NONCLUSTERED ([Nombre] ASC)
);

