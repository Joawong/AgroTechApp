CREATE TABLE [agro].[Mortalidad] (
    [MortalidadId] BIGINT         IDENTITY (1, 1) NOT NULL,
    [AnimalId]     BIGINT         NOT NULL,
    [Fecha]        DATETIME2 (7)  DEFAULT (sysdatetime()) NOT NULL,
    [Causa]        NVARCHAR (200) NULL,
    [Observacion]  NVARCHAR (300) NULL,
    PRIMARY KEY CLUSTERED ([MortalidadId] ASC),
    FOREIGN KEY ([AnimalId]) REFERENCES [agro].[Animal] ([AnimalId])
);

