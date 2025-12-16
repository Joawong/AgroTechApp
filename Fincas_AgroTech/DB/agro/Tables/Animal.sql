CREATE TABLE [agro].[Animal] (
    [AnimalId]        BIGINT          IDENTITY (1, 1) NOT NULL,
    [FincaId]         BIGINT          NOT NULL,
    [Arete]           NVARCHAR (80)   NOT NULL,
    [Nombre]          NVARCHAR (120)  NULL,
    [Sexo]            CHAR (1)        NOT NULL,
    [RazaId]          INT             NULL,
    [FechaNacimiento] DATE            NULL,
    [PesoNacimiento]  DECIMAL (18, 3) NULL,
    [Estado]          NVARCHAR (30)   DEFAULT (N'Activo') NOT NULL,
    [MadreId]         BIGINT          NULL,
    [PadreId]         BIGINT          NULL,
    [LoteAnimalId]    BIGINT          NULL,
    [CostoCompra]     DECIMAL (18, 2) NULL,
    [PrecioVenta]     DECIMAL (18, 2) NULL,
    [FechaVenta]      DATE            NULL,
    PRIMARY KEY CLUSTERED ([AnimalId] ASC),
    CHECK ([Estado]=N'Trasladado' OR [Estado]=N'Muerto' OR [Estado]=N'Vendido' OR [Estado]=N'Activo'),
    CONSTRAINT [CK_Animal_Sexo] CHECK ([Sexo]='H' OR [Sexo]='M'),
    FOREIGN KEY ([FincaId]) REFERENCES [agro].[Finca] ([FincaId]),
    FOREIGN KEY ([LoteAnimalId]) REFERENCES [agro].[LoteAnimal] ([LoteAnimalId]),
    FOREIGN KEY ([MadreId]) REFERENCES [agro].[Animal] ([AnimalId]),
    FOREIGN KEY ([PadreId]) REFERENCES [agro].[Animal] ([AnimalId]),
    FOREIGN KEY ([RazaId]) REFERENCES [agro].[Raza] ([RazaId]),
    UNIQUE NONCLUSTERED ([FincaId] ASC, [Arete] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_Animal_Finca_Arete]
    ON [agro].[Animal]([FincaId] ASC, [Arete] ASC);

