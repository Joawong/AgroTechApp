CREATE TABLE [agro].[Finca] (
    [FincaId]       BIGINT         IDENTITY (1, 1) NOT NULL,
    [Nombre]        NVARCHAR (200) NOT NULL,
    [Ubicacion]     NVARCHAR (300) NULL,
    [Activa]        BIT            DEFAULT ((1)) NOT NULL,
    [FechaCreacion] DATETIME2 (7)  DEFAULT (sysdatetime()) NOT NULL,
    PRIMARY KEY CLUSTERED ([FincaId] ASC),
    UNIQUE NONCLUSTERED ([Nombre] ASC)
);

