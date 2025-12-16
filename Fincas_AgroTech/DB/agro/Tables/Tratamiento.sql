CREATE TABLE [agro].[Tratamiento] (
    [TratamientoId] BIGINT         IDENTITY (1, 1) NOT NULL,
    [FincaId]       BIGINT         NOT NULL,
    [TipoTratId]    INT            NOT NULL,
    [AnimalId]      BIGINT         NULL,
    [LoteAnimalId]  BIGINT         NULL,
    [Fecha]         DATETIME2 (7)  DEFAULT (sysdatetime()) NOT NULL,
    [InsumoId]      BIGINT         NULL,
    [LoteId]        BIGINT         NULL,
    [Dosis]         NVARCHAR (60)  NULL,
    [Via]           NVARCHAR (40)  NULL,
    [Responsable]   NVARCHAR (200) NULL,
    [Observacion]   NVARCHAR (300) NULL,
    PRIMARY KEY CLUSTERED ([TratamientoId] ASC),
    FOREIGN KEY ([AnimalId]) REFERENCES [agro].[Animal] ([AnimalId]),
    FOREIGN KEY ([FincaId]) REFERENCES [agro].[Finca] ([FincaId]),
    FOREIGN KEY ([InsumoId]) REFERENCES [agro].[Insumo] ([InsumoId]),
    FOREIGN KEY ([LoteAnimalId]) REFERENCES [agro].[LoteAnimal] ([LoteAnimalId]),
    FOREIGN KEY ([LoteId]) REFERENCES [agro].[InsumoLote] ([LoteId]),
    FOREIGN KEY ([TipoTratId]) REFERENCES [agro].[TipoTratamiento] ([TipoTratId])
);


GO
CREATE NONCLUSTERED INDEX [IX_Tratamiento_Finca_Fecha]
    ON [agro].[Tratamiento]([FincaId] ASC, [Fecha] ASC);

