CREATE TABLE [agro].[TipoTratamiento] (
    [TipoTratId] INT            IDENTITY (1, 1) NOT NULL,
    [Nombre]     NVARCHAR (120) NOT NULL,
    PRIMARY KEY CLUSTERED ([TipoTratId] ASC),
    UNIQUE NONCLUSTERED ([Nombre] ASC)
);

