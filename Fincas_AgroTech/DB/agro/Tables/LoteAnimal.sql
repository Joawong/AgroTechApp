CREATE TABLE [agro].[LoteAnimal] (
    [LoteAnimalId] BIGINT         IDENTITY (1, 1) NOT NULL,
    [FincaId]      BIGINT         NOT NULL,
    [Nombre]       NVARCHAR (120) NOT NULL,
    [Tipo]         NVARCHAR (60)  NOT NULL,
    PRIMARY KEY CLUSTERED ([LoteAnimalId] ASC),
    FOREIGN KEY ([FincaId]) REFERENCES [agro].[Finca] ([FincaId]),
    UNIQUE NONCLUSTERED ([FincaId] ASC, [Nombre] ASC)
);

