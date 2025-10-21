CREATE TABLE [agro].[Pesaje] (
    [PesajeId]    BIGINT          IDENTITY (1, 1) NOT NULL,
    [AnimalId]    BIGINT          NOT NULL,
    [Fecha]       DATETIME2 (7)   NOT NULL,
    [PesoKg]      DECIMAL (18, 3) NOT NULL,
    [Observacion] NVARCHAR (300)  NULL,
    PRIMARY KEY CLUSTERED ([PesajeId] ASC),
    FOREIGN KEY ([AnimalId]) REFERENCES [agro].[Animal] ([AnimalId]),
    UNIQUE NONCLUSTERED ([AnimalId] ASC, [Fecha] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_Pesaje_Animal_Fecha]
    ON [agro].[Pesaje]([AnimalId] ASC, [Fecha] DESC);

