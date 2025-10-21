CREATE TABLE [agro].[User] (
    [UserId]        BIGINT          IDENTITY (1, 1) NOT NULL,
    [Nombre]        NVARCHAR (200)  NOT NULL,
    [Email]         NVARCHAR (200)  NOT NULL,
    [HashPassword]  VARBINARY (MAX) NOT NULL,
    [RolId]         INT             NOT NULL,
    [Activo]        BIT             DEFAULT ((1)) NOT NULL,
    [FechaCreacion] DATETIME2 (7)   DEFAULT (sysdatetime()) NOT NULL,
    PRIMARY KEY CLUSTERED ([UserId] ASC),
    FOREIGN KEY ([RolId]) REFERENCES [agro].[Rol] ([RolId]),
    UNIQUE NONCLUSTERED ([Email] ASC)
);

