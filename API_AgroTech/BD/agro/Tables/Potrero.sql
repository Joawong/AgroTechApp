CREATE TABLE [agro].[Potrero] (
    [PotreroId] BIGINT          IDENTITY (1, 1) NOT NULL,
    [FincaId]   BIGINT          NOT NULL,
    [Nombre]    NVARCHAR (120)  NOT NULL,
    [Hectareas] DECIMAL (10, 2) NULL,
    [Activo]    BIT             DEFAULT ((1)) NOT NULL,
    PRIMARY KEY CLUSTERED ([PotreroId] ASC),
    FOREIGN KEY ([FincaId]) REFERENCES [agro].[Finca] ([FincaId]),
    UNIQUE NONCLUSTERED ([FincaId] ASC, [Nombre] ASC)
);

