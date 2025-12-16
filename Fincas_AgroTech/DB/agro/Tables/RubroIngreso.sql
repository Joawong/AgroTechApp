CREATE TABLE [agro].[RubroIngreso] (
    [RubroIngresoId] INT            IDENTITY (1, 1) NOT NULL,
    [Nombre]         NVARCHAR (120) NOT NULL,
    PRIMARY KEY CLUSTERED ([RubroIngresoId] ASC),
    UNIQUE NONCLUSTERED ([Nombre] ASC)
);

