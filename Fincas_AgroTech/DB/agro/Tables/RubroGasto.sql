CREATE TABLE [agro].[RubroGasto] (
    [RubroGastoId] INT            IDENTITY (1, 1) NOT NULL,
    [Nombre]       NVARCHAR (120) NOT NULL,
    PRIMARY KEY CLUSTERED ([RubroGastoId] ASC),
    UNIQUE NONCLUSTERED ([Nombre] ASC)
);

