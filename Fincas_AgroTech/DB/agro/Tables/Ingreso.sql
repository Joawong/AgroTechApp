CREATE TABLE [agro].[Ingreso] (
    [IngresoId]      BIGINT          IDENTITY (1, 1) NOT NULL,
    [FincaId]        BIGINT          NOT NULL,
    [RubroIngresoId] INT             NOT NULL,
    [Fecha]          DATE            NOT NULL,
    [Monto]          DECIMAL (18, 2) NOT NULL,
    [Descripcion]    NVARCHAR (300)  NULL,
    [AnimalId]       BIGINT          NULL,
    PRIMARY KEY CLUSTERED ([IngresoId] ASC),
    FOREIGN KEY ([AnimalId]) REFERENCES [agro].[Animal] ([AnimalId]),
    FOREIGN KEY ([FincaId]) REFERENCES [agro].[Finca] ([FincaId]),
    FOREIGN KEY ([RubroIngresoId]) REFERENCES [agro].[RubroIngreso] ([RubroIngresoId])
);


GO
CREATE NONCLUSTERED INDEX [IX_Ingreso_Finca_Fecha]
    ON [agro].[Ingreso]([FincaId] ASC, [Fecha] ASC);

