CREATE TABLE [agro].[MovimientoAnimal] (
    [MovAnimalId] BIGINT         IDENTITY (1, 1) NOT NULL,
    [AnimalId]    BIGINT         NOT NULL,
    [PotreroId]   BIGINT         NULL,
    [FechaDesde]  DATETIME2 (7)  NOT NULL,
    [FechaHasta]  DATETIME2 (7)  NULL,
    [Observacion] NVARCHAR (300) NULL,
    PRIMARY KEY CLUSTERED ([MovAnimalId] ASC),
    FOREIGN KEY ([AnimalId]) REFERENCES [agro].[Animal] ([AnimalId]),
    FOREIGN KEY ([PotreroId]) REFERENCES [agro].[Potrero] ([PotreroId])
);

