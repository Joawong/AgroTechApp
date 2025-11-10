/* =========================
   0) BASE DE DATOS & ESQUEMA
   ========================= */
IF DB_ID('AgroTech') IS NULL
BEGIN
  CREATE DATABASE AgroTech;
END
GO

USE AgroTech;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'agro')
BEGIN
  EXEC('CREATE SCHEMA agro');
END
GO


/* =========================
   1) Fincas y Usuarios
   ========================= */
CREATE TABLE agro.Finca (
  FincaId       BIGINT IDENTITY PRIMARY KEY,
  Nombre        NVARCHAR(200) NOT NULL,
  Ubicacion     NVARCHAR(300) NULL,
  Activa        BIT NOT NULL DEFAULT(1),
  FechaCreacion DATETIME2 NOT NULL DEFAULT(SYSDATETIME()),
  UNIQUE (Nombre)
);

CREATE TABLE agro.Rol (
  RolId   INT IDENTITY PRIMARY KEY,
  Nombre  NVARCHAR(100) NOT NULL UNIQUE  -- Admin, Operario, Veterinario, Finanzas
);

CREATE TABLE agro.[User] (
  UserId        BIGINT IDENTITY PRIMARY KEY,
  Nombre        NVARCHAR(200) NOT NULL,
  Email         NVARCHAR(200) NOT NULL UNIQUE,
  HashPassword  VARBINARY(MAX) NOT NULL,
  RolId         INT NOT NULL REFERENCES agro.Rol(RolId),
  Activo        BIT NOT NULL DEFAULT(1),
  FechaCreacion DATETIME2 NOT NULL DEFAULT(SYSDATETIME())
);

-- Acceso del usuario a sus fincas (un granjero con varias fincas)
CREATE TABLE agro.UserFinca (
  UserId  BIGINT NOT NULL REFERENCES agro.[User](UserId),
  FincaId BIGINT NOT NULL REFERENCES agro.Finca(FincaId),
  PRIMARY KEY (UserId, FincaId)
);

/* =========================
   2) Catálogos mínimos
   ========================= */
CREATE TABLE agro.UnidadMedida (
  UnidadId INT IDENTITY PRIMARY KEY,
  Codigo   NVARCHAR(30) NOT NULL UNIQUE,  -- KG, LT, UND
  Nombre   NVARCHAR(80) NOT NULL
);

CREATE TABLE agro.CategoriaInsumo (
  CategoriaId INT IDENTITY PRIMARY KEY,
  Nombre      NVARCHAR(120) NOT NULL UNIQUE  -- Alimento, Medicina, Suplemento, Combustible, Otros
);

CREATE TABLE agro.Raza (
  RazaId INT IDENTITY PRIMARY KEY,
  Nombre NVARCHAR(120) NOT NULL UNIQUE
);

CREATE TABLE agro.TipoMovimientoInventario (
  TipoId INT IDENTITY PRIMARY KEY,
  Nombre NVARCHAR(80) NOT NULL UNIQUE      -- Compra, Consumo, Ajuste+, Ajuste-
);

CREATE TABLE agro.TipoTratamiento (
  TipoTratId INT IDENTITY PRIMARY KEY,
  Nombre     NVARCHAR(120) NOT NULL UNIQUE -- Vacunación, Antibiótico, Desparasitación, etc.
);

CREATE TABLE agro.RubroGasto (
  RubroGastoId INT IDENTITY PRIMARY KEY,
  Nombre       NVARCHAR(120) NOT NULL UNIQUE -- Alimento, Medicina, ManoObra, Mantenimiento, Etc.
);

CREATE TABLE agro.RubroIngreso (
  RubroIngresoId INT IDENTITY PRIMARY KEY,
  Nombre         NVARCHAR(120) NOT NULL UNIQUE -- VentaAnimal, VentaLeche, Otros
);

/* =========================
   3) Inventarios (simple y claro)
   ========================= */
CREATE TABLE agro.Insumo (
  InsumoId    BIGINT IDENTITY PRIMARY KEY,
  FincaId     BIGINT NOT NULL REFERENCES agro.Finca(FincaId),
  CategoriaId INT NOT NULL REFERENCES agro.CategoriaInsumo(CategoriaId),
  Nombre      NVARCHAR(200) NOT NULL,
  UnidadId    INT NOT NULL REFERENCES agro.UnidadMedida(UnidadId),
  StockMinimo DECIMAL(18,3) NOT NULL DEFAULT(0),
  Activo      BIT NOT NULL DEFAULT(1),
  UNIQUE (FincaId, Nombre)
);

-- Lotes y vencimientos (para medicinas/alimentos)
CREATE TABLE agro.InsumoLote (
  LoteId          BIGINT IDENTITY PRIMARY KEY,
  InsumoId        BIGINT NOT NULL REFERENCES agro.Insumo(InsumoId),
  CodigoLote      NVARCHAR(120) NULL,
  FechaVencimiento DATE NULL,
  UNIQUE (InsumoId, CodigoLote)
);

CREATE TABLE agro.MovimientoInventario (
  MovId         BIGINT IDENTITY PRIMARY KEY,
  FincaId       BIGINT NOT NULL REFERENCES agro.Finca(FincaId),
  InsumoId      BIGINT NOT NULL REFERENCES agro.Insumo(InsumoId),
  LoteId        BIGINT NULL REFERENCES agro.InsumoLote(LoteId),
  TipoId        INT NOT NULL REFERENCES agro.TipoMovimientoInventario(TipoId),
  Cantidad      DECIMAL(18,3) NOT NULL,       -- + entrada, - salida
  CostoUnitario DECIMAL(18,4) NULL,           -- solo entradas/ajustes+
  Fecha         DATETIME2 NOT NULL DEFAULT(SYSDATETIME()),
  Observacion   NVARCHAR(300) NULL,
  CHECK (Cantidad <> 0)
);
CREATE INDEX IX_MovInv_Insumo_Fecha ON agro.MovimientoInventario(InsumoId, Fecha);

/* =========================
   4) Animales / Lotes / Potreros
   ========================= */
CREATE TABLE agro.LoteAnimal (
  LoteAnimalId BIGINT IDENTITY PRIMARY KEY,
  FincaId      BIGINT NOT NULL REFERENCES agro.Finca(FincaId),
  Nombre       NVARCHAR(120) NOT NULL,
  Tipo         NVARCHAR(60) NOT NULL,  -- Terneros, Novillas, Vacas, Toros, Engorde, etc.
  UNIQUE (FincaId, Nombre)
);

CREATE TABLE agro.Potrero (
  PotreroId BIGINT IDENTITY PRIMARY KEY,
  FincaId   BIGINT NOT NULL REFERENCES agro.Finca(FincaId),
  Nombre    NVARCHAR(120) NOT NULL,
  Hectareas DECIMAL(10,2) NULL,
  Activo    BIT NOT NULL DEFAULT(1),
  UNIQUE (FincaId, Nombre)
);

CREATE TABLE agro.Animal (
  AnimalId        BIGINT IDENTITY PRIMARY KEY,
  FincaId         BIGINT NOT NULL REFERENCES agro.Finca(FincaId),
  Arete           NVARCHAR(80) NOT NULL,
  Nombre          NVARCHAR(120) NULL,
  Sexo            CHAR(1) NOT NULL CHECK (Sexo IN ('M','F')),
  RazaId          INT NULL REFERENCES agro.Raza(RazaId),
  FechaNacimiento DATE NULL,
  PesoNacimiento  DECIMAL(18,3) NULL,
  Estado          NVARCHAR(30) NOT NULL DEFAULT(N'Activo')
                  CHECK (Estado IN (N'Activo',N'Vendido',N'Muerto',N'Trasladado')),
  MadreId         BIGINT NULL REFERENCES agro.Animal(AnimalId),
  PadreId         BIGINT NULL REFERENCES agro.Animal(AnimalId),
  LoteAnimalId    BIGINT NULL REFERENCES agro.LoteAnimal(LoteAnimalId),
  UNIQUE (FincaId, Arete)
);

-- Ubicación en potrero (histórico simple)
CREATE TABLE agro.MovimientoAnimal (
  MovAnimalId BIGINT IDENTITY PRIMARY KEY,
  AnimalId    BIGINT NOT NULL REFERENCES agro.Animal(AnimalId),
  PotreroId   BIGINT NULL REFERENCES agro.Potrero(PotreroId),
  FechaDesde  DATETIME2 NOT NULL,
  FechaHasta  DATETIME2 NULL,  -- NULL = ubicación actual
  Observacion NVARCHAR(300) NULL
);

/* =========================
   5) Sanidad y Bienestar (simple)
   ========================= */
CREATE TABLE agro.Tratamiento (
  TratamientoId BIGINT IDENTITY PRIMARY KEY,
  FincaId       BIGINT NOT NULL REFERENCES agro.Finca(FincaId),
  TipoTratId    INT NOT NULL REFERENCES agro.TipoTratamiento(TipoTratId),
  AnimalId      BIGINT NULL REFERENCES agro.Animal(AnimalId),
  LoteAnimalId  BIGINT NULL REFERENCES agro.LoteAnimal(LoteAnimalId),
  Fecha         DATETIME2 NOT NULL DEFAULT(SYSDATETIME()),
  InsumoId      BIGINT NULL REFERENCES agro.Insumo(InsumoId),  -- medicina usada
  LoteId        BIGINT NULL REFERENCES agro.InsumoLote(LoteId),
  Dosis         NVARCHAR(60) NULL,
  Via           NVARCHAR(40) NULL,    -- IM, SC, Oral...
  Responsable   NVARCHAR(200) NULL,   -- nombre/alias del aplicador
  Observacion   NVARCHAR(300) NULL
);

-- Periodos de retiro (para leche/carne)
CREATE TABLE agro.PeriodoRetiro (
  RetiroId      BIGINT IDENTITY PRIMARY KEY,
  TratamientoId BIGINT NOT NULL REFERENCES agro.Tratamiento(TratamientoId),
  FechaDesde    DATE NOT NULL,
  FechaHasta    DATE NOT NULL,
  Producto      NVARCHAR(40) NOT NULL CHECK (Producto IN (N'Leche', N'Carne'))
);

-- Mortalidad (registro básico)
CREATE TABLE agro.Mortalidad (
  MortalidadId BIGINT IDENTITY PRIMARY KEY,
  AnimalId     BIGINT NOT NULL REFERENCES agro.Animal(AnimalId),
  Fecha        DATETIME2 NOT NULL DEFAULT(SYSDATETIME()),
  Causa        NVARCHAR(200) NULL,
  Observacion  NVARCHAR(300) NULL
);

/* =========================
   6) Producción y Crecimiento (pesajes)
   ========================= */
CREATE TABLE agro.Pesaje (
  PesajeId    BIGINT IDENTITY PRIMARY KEY,
  AnimalId    BIGINT NOT NULL REFERENCES agro.Animal(AnimalId),
  Fecha       DATETIME2 NOT NULL,
  PesoKg      DECIMAL(18,3) NOT NULL,
  Observacion NVARCHAR(300) NULL,
  UNIQUE (AnimalId, Fecha)
);
CREATE INDEX IX_Pesaje_Animal_Fecha ON agro.Pesaje(AnimalId, Fecha DESC);

/* =========================
   7) Costos y Finanzas (simple)
   ========================= */
-- Gastos por rubro (opcionalmente asociados a animal/potrero/insumo)
CREATE TABLE agro.Gasto (
  GastoId     BIGINT IDENTITY PRIMARY KEY,
  FincaId     BIGINT NOT NULL REFERENCES agro.Finca(FincaId),
  RubroGastoId INT NOT NULL REFERENCES agro.RubroGasto(RubroGastoId),
  Fecha       DATE NOT NULL,
  Monto       DECIMAL(18,2) NOT NULL,
  Descripcion NVARCHAR(300) NULL,
  AnimalId    BIGINT NULL REFERENCES agro.Animal(AnimalId),
  PotreroId   BIGINT NULL REFERENCES agro.Potrero(PotreroId),
  InsumoId    BIGINT NULL REFERENCES agro.Insumo(InsumoId)
);

-- Ingresos por rubro (ej: venta de animales, leche)
CREATE TABLE agro.Ingreso (
  IngresoId      BIGINT IDENTITY PRIMARY KEY,
  FincaId        BIGINT NOT NULL REFERENCES agro.Finca(FincaId),
  RubroIngresoId INT NOT NULL REFERENCES agro.RubroIngreso(RubroIngresoId),
  Fecha          DATE NOT NULL,
  Monto          DECIMAL(18,2) NOT NULL,
  Descripcion    NVARCHAR(300) NULL,
  AnimalId       BIGINT NULL REFERENCES agro.Animal(AnimalId) -- útil para ventas
);

/* =========================
   8) Índices útiles
   ========================= */
CREATE INDEX IX_Animal_Finca_Arete ON agro.Animal(FincaId, Arete);
CREATE INDEX IX_Tratamiento_Finca_Fecha ON agro.Tratamiento(FincaId, Fecha);
CREATE INDEX IX_Gasto_Finca_Fecha ON agro.Gasto(FincaId, Fecha);
CREATE INDEX IX_Ingreso_Finca_Fecha ON agro.Ingreso(FincaId, Fecha);

-- Vista de stock (por finca e insumo)
CREATE OR ALTER VIEW agro.vStockPorInsumo AS
SELECT m.FincaId, m.InsumoId, SUM(m.Cantidad) AS Stock
FROM agro.MovimientoInventario m
GROUP BY m.FincaId, m.InsumoId;
go 


CREATE OR ALTER VIEW agro.vStockPorLote AS
SELECT m.FincaId, m.InsumoId, m.LoteId, SUM(m.Cantidad) AS Stock
FROM agro.MovimientoInventario m
GROUP BY m.FincaId, m.InsumoId, m.LoteId;
go


-- Un insumo con mismo Nombre por Finca
CREATE UNIQUE INDEX UX_Insumo_Finca_Nombre
ON agro.Insumo (FincaId, Nombre);


-- Semilla para tipos
INSERT INTO agro.TipoMovimientoInventario(Nombre) 
SELECT v FROM (VALUES ('Compra'),('Consumo'),('Ajuste+'),('Ajuste-')) AS t(v)
WHERE NOT EXISTS (SELECT 1 FROM agro.TipoMovimientoInventario);