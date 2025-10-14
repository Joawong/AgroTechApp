CREATE TABLE [agro].[Rol] (
    [RolId]  INT            IDENTITY (1, 1) NOT NULL,
    [Nombre] NVARCHAR (100) NOT NULL,
    PRIMARY KEY CLUSTERED ([RolId] ASC),
    UNIQUE NONCLUSTERED ([Nombre] ASC)
);

