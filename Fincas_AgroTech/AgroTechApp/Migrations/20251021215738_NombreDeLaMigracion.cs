using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroTechApp.Migrations
{
    /// <inheritdoc />
    public partial class NombreDeLaMigracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "agro");

            migrationBuilder.CreateTable(
                name: "CategoriaInsumo",
                schema: "agro",
                columns: table => new
                {
                    CategoriaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Categori__F353C1E5D488D06F", x => x.CategoriaId);
                });

            migrationBuilder.CreateTable(
                name: "Finca",
                schema: "agro",
                columns: table => new
                {
                    FincaId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Ubicacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Finca__2CBEEB69224D34AC", x => x.FincaId);
                });

            migrationBuilder.CreateTable(
                name: "Raza",
                schema: "agro",
                columns: table => new
                {
                    RazaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Raza__39A423D838BEA012", x => x.RazaId);
                });

            migrationBuilder.CreateTable(
                name: "Rol",
                schema: "agro",
                columns: table => new
                {
                    RolId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime", nullable: true),
                    UsuarioCrea = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsuarioModifica = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Rol__F92302F1F2A4F3A0", x => x.RolId);
                });

            migrationBuilder.CreateTable(
                name: "RubroGasto",
                schema: "agro",
                columns: table => new
                {
                    RubroGastoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RubroGas__7FAB05BF701B8B49", x => x.RubroGastoId);
                });

            migrationBuilder.CreateTable(
                name: "RubroIngreso",
                schema: "agro",
                columns: table => new
                {
                    RubroIngresoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RubroIng__4F74782FA8C12E33", x => x.RubroIngresoId);
                });

            migrationBuilder.CreateTable(
                name: "TipoMovimientoInventario",
                schema: "agro",
                columns: table => new
                {
                    TipoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TipoMovi__97099EB7ADD6A42D", x => x.TipoId);
                });

            migrationBuilder.CreateTable(
                name: "TipoTratamiento",
                schema: "agro",
                columns: table => new
                {
                    TipoTratId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TipoTrat__627E84A29673BD4B", x => x.TipoTratId);
                });

            migrationBuilder.CreateTable(
                name: "UnidadMedida",
                schema: "agro",
                columns: table => new
                {
                    UnidadId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UnidadMe__C6F324D6B55F7B5A", x => x.UnidadId);
                });

            migrationBuilder.CreateTable(
                name: "LoteAnimal",
                schema: "agro",
                columns: table => new
                {
                    LoteAnimalId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FincaId = table.Column<long>(type: "bigint", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__LoteAnim__BB31C7B19E2DFE0B", x => x.LoteAnimalId);
                    table.ForeignKey(
                        name: "FK__LoteAnima__Finca__70DDC3D8",
                        column: x => x.FincaId,
                        principalSchema: "agro",
                        principalTable: "Finca",
                        principalColumn: "FincaId");
                });

            migrationBuilder.CreateTable(
                name: "Potrero",
                schema: "agro",
                columns: table => new
                {
                    PotreroId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FincaId = table.Column<long>(type: "bigint", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Hectareas = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Potrero__673CB670820876E7", x => x.PotreroId);
                    table.ForeignKey(
                        name: "FK__Potrero__FincaId__74AE54BC",
                        column: x => x.FincaId,
                        principalSchema: "agro",
                        principalTable: "Finca",
                        principalColumn: "FincaId");
                });

            migrationBuilder.CreateTable(
                name: "User",
                schema: "agro",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HashPassword = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    RolId = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__User__1788CC4CCC96A2F8", x => x.UserId);
                    table.ForeignKey(
                        name: "FK__User__RolId__403A8C7D",
                        column: x => x.RolId,
                        principalSchema: "agro",
                        principalTable: "Rol",
                        principalColumn: "RolId");
                });

            migrationBuilder.CreateTable(
                name: "Insumo",
                schema: "agro",
                columns: table => new
                {
                    InsumoId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FincaId = table.Column<long>(type: "bigint", nullable: false),
                    CategoriaId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnidadId = table.Column<int>(type: "int", nullable: false),
                    StockMinimo = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Insumo__C10BE9561AEBE722", x => x.InsumoId);
                    table.ForeignKey(
                        name: "FK__Insumo__Categori__5EBF139D",
                        column: x => x.CategoriaId,
                        principalSchema: "agro",
                        principalTable: "CategoriaInsumo",
                        principalColumn: "CategoriaId");
                    table.ForeignKey(
                        name: "FK__Insumo__FincaId__5DCAEF64",
                        column: x => x.FincaId,
                        principalSchema: "agro",
                        principalTable: "Finca",
                        principalColumn: "FincaId");
                    table.ForeignKey(
                        name: "FK__Insumo__UnidadId__5FB337D6",
                        column: x => x.UnidadId,
                        principalSchema: "agro",
                        principalTable: "UnidadMedida",
                        principalColumn: "UnidadId");
                });

            migrationBuilder.CreateTable(
                name: "Animal",
                schema: "agro",
                columns: table => new
                {
                    AnimalId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FincaId = table.Column<long>(type: "bigint", nullable: false),
                    Arete = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Sexo = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: false),
                    RazaId = table.Column<int>(type: "int", nullable: true),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    PesoNacimiento = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Activo"),
                    MadreId = table.Column<long>(type: "bigint", nullable: true),
                    PadreId = table.Column<long>(type: "bigint", nullable: true),
                    LoteAnimalId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Animal__A21A73071EEB27B9", x => x.AnimalId);
                    table.ForeignKey(
                        name: "FK__Animal__FincaId__797309D9",
                        column: x => x.FincaId,
                        principalSchema: "agro",
                        principalTable: "Finca",
                        principalColumn: "FincaId");
                    table.ForeignKey(
                        name: "FK__Animal__LoteAnim__00200768",
                        column: x => x.LoteAnimalId,
                        principalSchema: "agro",
                        principalTable: "LoteAnimal",
                        principalColumn: "LoteAnimalId");
                    table.ForeignKey(
                        name: "FK__Animal__MadreId__7E37BEF6",
                        column: x => x.MadreId,
                        principalSchema: "agro",
                        principalTable: "Animal",
                        principalColumn: "AnimalId");
                    table.ForeignKey(
                        name: "FK__Animal__PadreId__7F2BE32F",
                        column: x => x.PadreId,
                        principalSchema: "agro",
                        principalTable: "Animal",
                        principalColumn: "AnimalId");
                    table.ForeignKey(
                        name: "FK__Animal__RazaId__7B5B524B",
                        column: x => x.RazaId,
                        principalSchema: "agro",
                        principalTable: "Raza",
                        principalColumn: "RazaId");
                });

            migrationBuilder.CreateTable(
                name: "UserFinca",
                schema: "agro",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    FincaId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserFinc__954322FA450C2252", x => new { x.UserId, x.FincaId });
                    table.ForeignKey(
                        name: "FK__UserFinca__Finca__45F365D3",
                        column: x => x.FincaId,
                        principalSchema: "agro",
                        principalTable: "Finca",
                        principalColumn: "FincaId");
                    table.ForeignKey(
                        name: "FK__UserFinca__UserI__44FF419A",
                        column: x => x.UserId,
                        principalSchema: "agro",
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "InsumoLote",
                schema: "agro",
                columns: table => new
                {
                    LoteId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InsumoId = table.Column<long>(type: "bigint", nullable: false),
                    CodigoLote = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__InsumoLo__E6EAE698D1B6F870", x => x.LoteId);
                    table.ForeignKey(
                        name: "FK__InsumoLot__Insum__656C112C",
                        column: x => x.InsumoId,
                        principalSchema: "agro",
                        principalTable: "Insumo",
                        principalColumn: "InsumoId");
                });

            migrationBuilder.CreateTable(
                name: "Gasto",
                schema: "agro",
                columns: table => new
                {
                    GastoId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FincaId = table.Column<long>(type: "bigint", nullable: false),
                    RubroGastoId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AnimalId = table.Column<long>(type: "bigint", nullable: true),
                    PotreroId = table.Column<long>(type: "bigint", nullable: true),
                    InsumoId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Gasto__815BB0F03A3DD153", x => x.GastoId);
                    table.ForeignKey(
                        name: "FK__Gasto__AnimalId__1CBC4616",
                        column: x => x.AnimalId,
                        principalSchema: "agro",
                        principalTable: "Animal",
                        principalColumn: "AnimalId");
                    table.ForeignKey(
                        name: "FK__Gasto__FincaId__1AD3FDA4",
                        column: x => x.FincaId,
                        principalSchema: "agro",
                        principalTable: "Finca",
                        principalColumn: "FincaId");
                    table.ForeignKey(
                        name: "FK__Gasto__InsumoId__1EA48E88",
                        column: x => x.InsumoId,
                        principalSchema: "agro",
                        principalTable: "Insumo",
                        principalColumn: "InsumoId");
                    table.ForeignKey(
                        name: "FK__Gasto__PotreroId__1DB06A4F",
                        column: x => x.PotreroId,
                        principalSchema: "agro",
                        principalTable: "Potrero",
                        principalColumn: "PotreroId");
                    table.ForeignKey(
                        name: "FK__Gasto__RubroGast__1BC821DD",
                        column: x => x.RubroGastoId,
                        principalSchema: "agro",
                        principalTable: "RubroGasto",
                        principalColumn: "RubroGastoId");
                });

            migrationBuilder.CreateTable(
                name: "Ingreso",
                schema: "agro",
                columns: table => new
                {
                    IngresoId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FincaId = table.Column<long>(type: "bigint", nullable: false),
                    RubroIngresoId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AnimalId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Ingreso__DBF0909A280B49FC", x => x.IngresoId);
                    table.ForeignKey(
                        name: "FK__Ingreso__AnimalI__236943A5",
                        column: x => x.AnimalId,
                        principalSchema: "agro",
                        principalTable: "Animal",
                        principalColumn: "AnimalId");
                    table.ForeignKey(
                        name: "FK__Ingreso__FincaId__2180FB33",
                        column: x => x.FincaId,
                        principalSchema: "agro",
                        principalTable: "Finca",
                        principalColumn: "FincaId");
                    table.ForeignKey(
                        name: "FK__Ingreso__RubroIn__22751F6C",
                        column: x => x.RubroIngresoId,
                        principalSchema: "agro",
                        principalTable: "RubroIngreso",
                        principalColumn: "RubroIngresoId");
                });

            migrationBuilder.CreateTable(
                name: "Mortalidad",
                schema: "agro",
                columns: table => new
                {
                    MortalidadId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnimalId = table.Column<long>(type: "bigint", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())"),
                    Causa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Mortalid__43C36E9707B3C619", x => x.MortalidadId);
                    table.ForeignKey(
                        name: "FK__Mortalida__Anima__1332DBDC",
                        column: x => x.AnimalId,
                        principalSchema: "agro",
                        principalTable: "Animal",
                        principalColumn: "AnimalId");
                });

            migrationBuilder.CreateTable(
                name: "MovimientoAnimal",
                schema: "agro",
                columns: table => new
                {
                    MovAnimalId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnimalId = table.Column<long>(type: "bigint", nullable: false),
                    PotreroId = table.Column<long>(type: "bigint", nullable: true),
                    FechaDesde = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaHasta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Movimien__DC34C30DB5F7E963", x => x.MovAnimalId);
                    table.ForeignKey(
                        name: "FK__Movimient__Anima__02FC7413",
                        column: x => x.AnimalId,
                        principalSchema: "agro",
                        principalTable: "Animal",
                        principalColumn: "AnimalId");
                    table.ForeignKey(
                        name: "FK__Movimient__Potre__03F0984C",
                        column: x => x.PotreroId,
                        principalSchema: "agro",
                        principalTable: "Potrero",
                        principalColumn: "PotreroId");
                });

            migrationBuilder.CreateTable(
                name: "Pesaje",
                schema: "agro",
                columns: table => new
                {
                    PesajeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnimalId = table.Column<long>(type: "bigint", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PesoKg = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Pesaje__03DC4C0867DE8B5B", x => x.PesajeId);
                    table.ForeignKey(
                        name: "FK__Pesaje__AnimalId__17F790F9",
                        column: x => x.AnimalId,
                        principalSchema: "agro",
                        principalTable: "Animal",
                        principalColumn: "AnimalId");
                });

            migrationBuilder.CreateTable(
                name: "MovimientoInventario",
                schema: "agro",
                columns: table => new
                {
                    MovId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FincaId = table.Column<long>(type: "bigint", nullable: false),
                    InsumoId = table.Column<long>(type: "bigint", nullable: false),
                    LoteId = table.Column<long>(type: "bigint", nullable: true),
                    TipoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())"),
                    Observacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Movimien__C4941F47F78D8641", x => x.MovId);
                    table.ForeignKey(
                        name: "FK__Movimient__Finca__68487DD7",
                        column: x => x.FincaId,
                        principalSchema: "agro",
                        principalTable: "Finca",
                        principalColumn: "FincaId");
                    table.ForeignKey(
                        name: "FK__Movimient__Insum__693CA210",
                        column: x => x.InsumoId,
                        principalSchema: "agro",
                        principalTable: "Insumo",
                        principalColumn: "InsumoId");
                    table.ForeignKey(
                        name: "FK__Movimient__LoteI__6A30C649",
                        column: x => x.LoteId,
                        principalSchema: "agro",
                        principalTable: "InsumoLote",
                        principalColumn: "LoteId");
                    table.ForeignKey(
                        name: "FK__Movimient__TipoI__6B24EA82",
                        column: x => x.TipoId,
                        principalSchema: "agro",
                        principalTable: "TipoMovimientoInventario",
                        principalColumn: "TipoId");
                });

            migrationBuilder.CreateTable(
                name: "Tratamiento",
                schema: "agro",
                columns: table => new
                {
                    TratamientoId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FincaId = table.Column<long>(type: "bigint", nullable: false),
                    TipoTratId = table.Column<int>(type: "int", nullable: false),
                    AnimalId = table.Column<long>(type: "bigint", nullable: true),
                    LoteAnimalId = table.Column<long>(type: "bigint", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())"),
                    InsumoId = table.Column<long>(type: "bigint", nullable: true),
                    LoteId = table.Column<long>(type: "bigint", nullable: true),
                    Dosis = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Via = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Responsable = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Tratamie__6CFB22659AC62484", x => x.TratamientoId);
                    table.ForeignKey(
                        name: "FK__Tratamien__Anima__08B54D69",
                        column: x => x.AnimalId,
                        principalSchema: "agro",
                        principalTable: "Animal",
                        principalColumn: "AnimalId");
                    table.ForeignKey(
                        name: "FK__Tratamien__Finca__06CD04F7",
                        column: x => x.FincaId,
                        principalSchema: "agro",
                        principalTable: "Finca",
                        principalColumn: "FincaId");
                    table.ForeignKey(
                        name: "FK__Tratamien__Insum__0B91BA14",
                        column: x => x.InsumoId,
                        principalSchema: "agro",
                        principalTable: "Insumo",
                        principalColumn: "InsumoId");
                    table.ForeignKey(
                        name: "FK__Tratamien__LoteA__09A971A2",
                        column: x => x.LoteAnimalId,
                        principalSchema: "agro",
                        principalTable: "LoteAnimal",
                        principalColumn: "LoteAnimalId");
                    table.ForeignKey(
                        name: "FK__Tratamien__LoteI__0C85DE4D",
                        column: x => x.LoteId,
                        principalSchema: "agro",
                        principalTable: "InsumoLote",
                        principalColumn: "LoteId");
                    table.ForeignKey(
                        name: "FK__Tratamien__TipoT__07C12930",
                        column: x => x.TipoTratId,
                        principalSchema: "agro",
                        principalTable: "TipoTratamiento",
                        principalColumn: "TipoTratId");
                });

            migrationBuilder.CreateTable(
                name: "PeriodoRetiro",
                schema: "agro",
                columns: table => new
                {
                    RetiroId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TratamientoId = table.Column<long>(type: "bigint", nullable: false),
                    FechaDesde = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaHasta = table.Column<DateOnly>(type: "date", nullable: false),
                    Producto = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PeriodoR__992834D8FCC4B4F1", x => x.RetiroId);
                    table.ForeignKey(
                        name: "FK__PeriodoRe__Trata__0F624AF8",
                        column: x => x.TratamientoId,
                        principalSchema: "agro",
                        principalTable: "Tratamiento",
                        principalColumn: "TratamientoId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Animal_Finca_Arete",
                schema: "agro",
                table: "Animal",
                columns: new[] { "FincaId", "Arete" });

            migrationBuilder.CreateIndex(
                name: "IX_Animal_LoteAnimalId",
                schema: "agro",
                table: "Animal",
                column: "LoteAnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_Animal_MadreId",
                schema: "agro",
                table: "Animal",
                column: "MadreId");

            migrationBuilder.CreateIndex(
                name: "IX_Animal_PadreId",
                schema: "agro",
                table: "Animal",
                column: "PadreId");

            migrationBuilder.CreateIndex(
                name: "IX_Animal_RazaId",
                schema: "agro",
                table: "Animal",
                column: "RazaId");

            migrationBuilder.CreateIndex(
                name: "UQ__Animal__4F10454D26C1F21C",
                schema: "agro",
                table: "Animal",
                columns: new[] { "FincaId", "Arete" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Categori__75E3EFCF786FDE5F",
                schema: "agro",
                table: "CategoriaInsumo",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Finca__75E3EFCF4156F071",
                schema: "agro",
                table: "Finca",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gasto_AnimalId",
                schema: "agro",
                table: "Gasto",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_Gasto_Finca_Fecha",
                schema: "agro",
                table: "Gasto",
                columns: new[] { "FincaId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Gasto_InsumoId",
                schema: "agro",
                table: "Gasto",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_Gasto_PotreroId",
                schema: "agro",
                table: "Gasto",
                column: "PotreroId");

            migrationBuilder.CreateIndex(
                name: "IX_Gasto_RubroGastoId",
                schema: "agro",
                table: "Gasto",
                column: "RubroGastoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingreso_AnimalId",
                schema: "agro",
                table: "Ingreso",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingreso_Finca_Fecha",
                schema: "agro",
                table: "Ingreso",
                columns: new[] { "FincaId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Ingreso_RubroIngresoId",
                schema: "agro",
                table: "Ingreso",
                column: "RubroIngresoId");

            migrationBuilder.CreateIndex(
                name: "IX_Insumo_CategoriaId",
                schema: "agro",
                table: "Insumo",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Insumo_UnidadId",
                schema: "agro",
                table: "Insumo",
                column: "UnidadId");

            migrationBuilder.CreateIndex(
                name: "UQ__Insumo__DBE0D59455BFB05C",
                schema: "agro",
                table: "Insumo",
                columns: new[] { "FincaId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__InsumoLo__0CF73AE1867A482D",
                schema: "agro",
                table: "InsumoLote",
                columns: new[] { "InsumoId", "CodigoLote" },
                unique: true,
                filter: "[CodigoLote] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ__LoteAnim__DBE0D594AE887473",
                schema: "agro",
                table: "LoteAnimal",
                columns: new[] { "FincaId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mortalidad_AnimalId",
                schema: "agro",
                table: "Mortalidad",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoAnimal_AnimalId",
                schema: "agro",
                table: "MovimientoAnimal",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoAnimal_PotreroId",
                schema: "agro",
                table: "MovimientoAnimal",
                column: "PotreroId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoInventario_FincaId",
                schema: "agro",
                table: "MovimientoInventario",
                column: "FincaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoInventario_LoteId",
                schema: "agro",
                table: "MovimientoInventario",
                column: "LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoInventario_TipoId",
                schema: "agro",
                table: "MovimientoInventario",
                column: "TipoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovInv_Insumo_Fecha",
                schema: "agro",
                table: "MovimientoInventario",
                columns: new[] { "InsumoId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_PeriodoRetiro_TratamientoId",
                schema: "agro",
                table: "PeriodoRetiro",
                column: "TratamientoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pesaje_Animal_Fecha",
                schema: "agro",
                table: "Pesaje",
                columns: new[] { "AnimalId", "Fecha" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "UQ__Pesaje__492ABBA30C060F52",
                schema: "agro",
                table: "Pesaje",
                columns: new[] { "AnimalId", "Fecha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Potrero__DBE0D5946768C80A",
                schema: "agro",
                table: "Potrero",
                columns: new[] { "FincaId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Raza__75E3EFCF0097E82F",
                schema: "agro",
                table: "Raza",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Rol__75E3EFCF9BA8E4C3",
                schema: "agro",
                table: "Rol",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__RubroGas__75E3EFCFFA349E2D",
                schema: "agro",
                table: "RubroGasto",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__RubroIng__75E3EFCF3BCD2A16",
                schema: "agro",
                table: "RubroIngreso",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__TipoMovi__75E3EFCF87EB3142",
                schema: "agro",
                table: "TipoMovimientoInventario",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__TipoTrat__75E3EFCF1A953E87",
                schema: "agro",
                table: "TipoTratamiento",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tratamiento_AnimalId",
                schema: "agro",
                table: "Tratamiento",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_Tratamiento_Finca_Fecha",
                schema: "agro",
                table: "Tratamiento",
                columns: new[] { "FincaId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Tratamiento_InsumoId",
                schema: "agro",
                table: "Tratamiento",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_Tratamiento_LoteAnimalId",
                schema: "agro",
                table: "Tratamiento",
                column: "LoteAnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_Tratamiento_LoteId",
                schema: "agro",
                table: "Tratamiento",
                column: "LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Tratamiento_TipoTratId",
                schema: "agro",
                table: "Tratamiento",
                column: "TipoTratId");

            migrationBuilder.CreateIndex(
                name: "UQ__UnidadMe__06370DAC4FE9848A",
                schema: "agro",
                table: "UnidadMedida",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_RolId",
                schema: "agro",
                table: "User",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "UQ__User__A9D1053427CA21FA",
                schema: "agro",
                table: "User",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFinca_FincaId",
                schema: "agro",
                table: "UserFinca",
                column: "FincaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Gasto",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "Ingreso",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "Mortalidad",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "MovimientoAnimal",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "MovimientoInventario",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "PeriodoRetiro",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "Pesaje",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "UserFinca",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "RubroGasto",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "RubroIngreso",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "Potrero",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "TipoMovimientoInventario",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "Tratamiento",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "User",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "Animal",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "InsumoLote",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "TipoTratamiento",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "Rol",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "LoteAnimal",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "Raza",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "Insumo",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "CategoriaInsumo",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "Finca",
                schema: "agro");

            migrationBuilder.DropTable(
                name: "UnidadMedida",
                schema: "agro");
        }
    }
}
