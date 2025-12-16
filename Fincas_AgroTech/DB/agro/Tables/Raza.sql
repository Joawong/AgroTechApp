CREATE TABLE [agro].[Raza] (
    [RazaId] INT            IDENTITY (1, 1) NOT NULL,
    [Nombre] NVARCHAR (120) NOT NULL,
    PRIMARY KEY CLUSTERED ([RazaId] ASC),
    UNIQUE NONCLUSTERED ([Nombre] ASC)
);

