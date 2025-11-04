CREATE TABLE [agro].[Rol] (
    [RolId]             INT              IDENTITY (1, 1) NOT NULL,
    [Nombre]            NVARCHAR (100)   NOT NULL,
    [FechaCreacion]     DATETIME         NULL,
    [FechaModificacion] DATETIME         NULL,
    [UsuarioCrea]       UNIQUEIDENTIFIER NULL,
    [UsuarioModifica]   UNIQUEIDENTIFIER NULL,
    PRIMARY KEY CLUSTERED ([RolId] ASC),
    UNIQUE NONCLUSTERED ([Nombre] ASC)
);

