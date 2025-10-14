CREATE TABLE [agro].[UnidadMedida] (
    [UnidadId] INT           IDENTITY (1, 1) NOT NULL,
    [Codigo]   NVARCHAR (30) NOT NULL,
    [Nombre]   NVARCHAR (80) NOT NULL,
    PRIMARY KEY CLUSTERED ([UnidadId] ASC),
    UNIQUE NONCLUSTERED ([Codigo] ASC)
);

