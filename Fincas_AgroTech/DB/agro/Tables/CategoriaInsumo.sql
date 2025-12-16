CREATE TABLE [agro].[CategoriaInsumo] (
    [CategoriaId] INT            IDENTITY (1, 1) NOT NULL,
    [Nombre]      NVARCHAR (120) NOT NULL,
    PRIMARY KEY CLUSTERED ([CategoriaId] ASC),
    UNIQUE NONCLUSTERED ([Nombre] ASC)
);

