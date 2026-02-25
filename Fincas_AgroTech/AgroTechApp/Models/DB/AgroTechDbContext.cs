using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AgroTechApp.Models.DB;

public partial class AgroTechDbContext : DbContext
{
    public AgroTechDbContext()
    {
    }

    public AgroTechDbContext(DbContextOptions<AgroTechDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Animal> Animals { get; set; }

    public virtual DbSet<CategoriaInsumo> CategoriaInsumos { get; set; }

    public virtual DbSet<Finca> Fincas { get; set; }

    public virtual DbSet<Gasto> Gastos { get; set; }

    public virtual DbSet<Ingreso> Ingresos { get; set; }

    public virtual DbSet<Insumo> Insumos { get; set; }

    public virtual DbSet<InsumoLote> InsumoLotes { get; set; }

    public virtual DbSet<LoteAnimal> LoteAnimals { get; set; }

    public virtual DbSet<Mortalidad> Mortalidads { get; set; }

    public virtual DbSet<MovimientoAnimal> MovimientoAnimals { get; set; }

    public virtual DbSet<MovimientoInventario> MovimientoInventarios { get; set; }

    public virtual DbSet<PeriodoRetiro> PeriodoRetiros { get; set; }

    public virtual DbSet<Pesaje> Pesajes { get; set; }

    public virtual DbSet<Potrero> Potreros { get; set; }

    public virtual DbSet<Raza> Razas { get; set; }

    public virtual DbSet<Rol> Rols { get; set; }

    public virtual DbSet<RubroGasto> RubroGastos { get; set; }

    public virtual DbSet<RubroIngreso> RubroIngresos { get; set; }

    public virtual DbSet<TipoMovimientoInventario> TipoMovimientoInventarios { get; set; }

    public virtual DbSet<TipoTratamiento> TipoTratamientos { get; set; }

    public virtual DbSet<Tratamiento> Tratamientos { get; set; }

    public virtual DbSet<UnidadMedidum> UnidadMedida { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public DbSet<UserFinca> UserFincas { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)

    {

        if (!optionsBuilder.IsConfigured)

        {

            // Solo como fallback, no debería llegar aquí

            optionsBuilder.UseSqlServer("Server=localhost;Database=AgroTech;Trusted_Connection=True;TrustServerCertificate=True;");

        }

    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Animal>(entity =>
        {
            entity.HasKey(e => e.AnimalId).HasName("PK__Animal__A21A73071EEB27B9");

            entity.ToTable("Animal", "agro");

            entity.HasIndex(e => new { e.FincaId, e.Arete }, "IX_Animal_Finca_Arete");

            entity.HasIndex(e => new { e.FincaId, e.Arete }, "UQ__Animal__4F10454D26C1F21C").IsUnique();

            entity.Property(e => e.Arete).HasMaxLength(80);
            entity.Property(e => e.Estado)
                .HasMaxLength(30)
                .HasDefaultValue("Activo");
            entity.Property(e => e.Nombre).HasMaxLength(120);
            entity.Property(e => e.PesoNacimiento).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Sexo)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.Finca).WithMany(p => p.Animals)
                .HasForeignKey(d => d.FincaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Animal__FincaId__797309D9");

            entity.HasOne(d => d.LoteAnimal).WithMany(p => p.Animals)
                .HasForeignKey(d => d.LoteAnimalId)
                .HasConstraintName("FK__Animal__LoteAnim__00200768");

            entity.HasOne(d => d.Madre).WithMany(p => p.InverseMadre)
                .HasForeignKey(d => d.MadreId)
                .HasConstraintName("FK__Animal__MadreId__7E37BEF6");

            entity.HasOne(d => d.Padre).WithMany(p => p.InversePadre)
                .HasForeignKey(d => d.PadreId)
                .HasConstraintName("FK__Animal__PadreId__7F2BE32F");

            entity.HasOne(d => d.Raza).WithMany(p => p.Animals)
                .HasForeignKey(d => d.RazaId)
                .HasConstraintName("FK__Animal__RazaId__7B5B524B");
        });

        modelBuilder.Entity<CategoriaInsumo>(entity =>
        {
            entity.HasKey(e => e.CategoriaId).HasName("PK__Categori__F353C1E5D488D06F");

            entity.ToTable("CategoriaInsumo", "agro");

            entity.HasIndex(e => e.Nombre, "UQ__Categori__75E3EFCF786FDE5F").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(120);
        });

        modelBuilder.Entity<Finca>(entity =>
        {
            entity.HasKey(e => e.FincaId).HasName("PK__Finca__2CBEEB69224D34AC");

            entity.ToTable("Finca", "agro");

            entity.HasIndex(e => e.Nombre, "UQ__Finca__75E3EFCF4156F071").IsUnique();

            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.Ubicacion).HasMaxLength(300);
        });

        modelBuilder.Entity<Gasto>(entity =>
        {
            entity.HasKey(e => e.GastoId).HasName("PK__Gasto__815BB0F03A3DD153");

            entity.ToTable("Gasto", "agro");

            entity.HasIndex(e => new { e.FincaId, e.Fecha }, "IX_Gasto_Finca_Fecha");

            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Animal).WithMany(p => p.Gastos)
                .HasForeignKey(d => d.AnimalId)
                .HasConstraintName("FK__Gasto__AnimalId__1CBC4616");

            entity.HasOne(d => d.Finca).WithMany(p => p.Gastos)
                .HasForeignKey(d => d.FincaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Gasto__FincaId__1AD3FDA4");

            entity.HasOne(d => d.Insumo).WithMany(p => p.Gastos)
                .HasForeignKey(d => d.InsumoId)
                .HasConstraintName("FK__Gasto__InsumoId__1EA48E88");

            entity.HasOne(d => d.Potrero).WithMany(p => p.Gastos)
                .HasForeignKey(d => d.PotreroId)
                .HasConstraintName("FK__Gasto__PotreroId__1DB06A4F");

            entity.HasOne(d => d.RubroGasto).WithMany(p => p.Gastos)
                .HasForeignKey(d => d.RubroGastoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Gasto__RubroGast__1BC821DD");
        });

        modelBuilder.Entity<Ingreso>(entity =>
        {
            entity.HasKey(e => e.IngresoId).HasName("PK__Ingreso__DBF0909A280B49FC");

            entity.ToTable("Ingreso", "agro");

            entity.HasIndex(e => new { e.FincaId, e.Fecha }, "IX_Ingreso_Finca_Fecha");

            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Animal).WithMany(p => p.Ingresos)
                .HasForeignKey(d => d.AnimalId)
                .HasConstraintName("FK__Ingreso__AnimalI__236943A5");

            entity.HasOne(d => d.Finca).WithMany(p => p.Ingresos)
                .HasForeignKey(d => d.FincaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ingreso__FincaId__2180FB33");

            entity.HasOne(d => d.RubroIngreso).WithMany(p => p.Ingresos)
                .HasForeignKey(d => d.RubroIngresoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ingreso__RubroIn__22751F6C");
        });

        modelBuilder.Entity<Insumo>(entity =>
        {
            entity.HasKey(e => e.InsumoId).HasName("PK__Insumo__C10BE9561AEBE722");

            entity.ToTable("Insumo", "agro");

            entity.HasIndex(e => new { e.FincaId, e.Nombre }, "UQ__Insumo__DBE0D59455BFB05C").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.StockMinimo).HasColumnType("decimal(18, 3)");

            entity.HasOne(d => d.Categoria).WithMany(p => p.Insumos)
                .HasForeignKey(d => d.CategoriaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Insumo__Categori__5EBF139D");

            entity.HasOne(d => d.Finca).WithMany(p => p.Insumos)
                .HasForeignKey(d => d.FincaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Insumo__FincaId__5DCAEF64");

            entity.HasOne(d => d.Unidad).WithMany(p => p.Insumos)
                .HasForeignKey(d => d.UnidadId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Insumo__UnidadId__5FB337D6");
        });

        modelBuilder.Entity<InsumoLote>(entity =>
        {
            entity.HasKey(e => e.LoteId).HasName("PK__InsumoLo__E6EAE698D1B6F870");

            entity.ToTable("InsumoLote", "agro");

            entity.HasIndex(e => new { e.InsumoId, e.CodigoLote }, "UQ__InsumoLo__0CF73AE1867A482D").IsUnique();

            entity.Property(e => e.CodigoLote).HasMaxLength(120);

            entity.HasOne(d => d.Insumo).WithMany(p => p.InsumoLotes)
                .HasForeignKey(d => d.InsumoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__InsumoLot__Insum__656C112C");
        });

        modelBuilder.Entity<LoteAnimal>(entity =>
        {
            entity.HasKey(e => e.LoteAnimalId).HasName("PK__LoteAnim__BB31C7B19E2DFE0B");

            entity.ToTable("LoteAnimal", "agro");

            entity.HasIndex(e => new { e.FincaId, e.Nombre }, "UQ__LoteAnim__DBE0D594AE887473").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(120);
            entity.Property(e => e.Tipo).HasMaxLength(60);

            entity.HasOne(d => d.Finca).WithMany(p => p.LoteAnimals)
                .HasForeignKey(d => d.FincaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LoteAnima__Finca__70DDC3D8");
        });

        modelBuilder.Entity<Mortalidad>(entity =>
        {
            entity.HasKey(e => e.MortalidadId).HasName("PK__Mortalid__43C36E9707B3C619");

            entity.ToTable("Mortalidad", "agro");

            entity.Property(e => e.Causa).HasMaxLength(200);
            entity.Property(e => e.Fecha).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Observacion).HasMaxLength(300);

            entity.HasOne(d => d.Animal).WithMany(p => p.Mortalidads)
                .HasForeignKey(d => d.AnimalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mortalida__Anima__1332DBDC");
        });

        modelBuilder.Entity<MovimientoAnimal>(entity =>
        {
            entity.HasKey(e => e.MovAnimalId).HasName("PK__Movimien__DC34C30DB5F7E963");

            entity.ToTable("MovimientoAnimal", "agro");

            entity.Property(e => e.Observacion).HasMaxLength(300);

            entity.HasOne(d => d.Animal).WithMany(p => p.MovimientoAnimals)
                .HasForeignKey(d => d.AnimalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Movimient__Anima__02FC7413");

            entity.HasOne(d => d.Potrero).WithMany(p => p.MovimientoAnimals)
                .HasForeignKey(d => d.PotreroId)
                .HasConstraintName("FK__Movimient__Potre__03F0984C");
        });

        modelBuilder.Entity<MovimientoInventario>(entity =>
        {
            entity.HasKey(e => e.MovId).HasName("PK__Movimien__C4941F47F78D8641");

            entity.ToTable("MovimientoInventario", "agro");

            entity.HasIndex(e => new { e.InsumoId, e.Fecha }, "IX_MovInv_Insumo_Fecha");

            entity.Property(e => e.Cantidad).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.CostoUnitario).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Fecha).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Observacion).HasMaxLength(300);

            entity.HasOne(d => d.Finca).WithMany(p => p.MovimientoInventarios)
                .HasForeignKey(d => d.FincaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Movimient__Finca__68487DD7");

            entity.HasOne(d => d.Insumo).WithMany(p => p.MovimientoInventarios)
                .HasForeignKey(d => d.InsumoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Movimient__Insum__693CA210");

            entity.HasOne(d => d.Lote).WithMany(p => p.MovimientoInventarios)
                .HasForeignKey(d => d.LoteId)
                .HasConstraintName("FK__Movimient__LoteI__6A30C649");

            entity.HasOne(d => d.Tipo).WithMany(p => p.MovimientoInventarios)
                .HasForeignKey(d => d.TipoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Movimient__TipoI__6B24EA82");
        });

        modelBuilder.Entity<PeriodoRetiro>(entity =>
        {
            entity.HasKey(e => e.RetiroId).HasName("PK__PeriodoR__992834D8FCC4B4F1");

            entity.ToTable("PeriodoRetiro", "agro");

            entity.Property(e => e.Producto).HasMaxLength(40);

            entity.HasOne(d => d.Tratamiento).WithMany(p => p.PeriodoRetiros)
                .HasForeignKey(d => d.TratamientoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PeriodoRe__Trata__0F624AF8");
        });

        modelBuilder.Entity<Pesaje>(entity =>
        {
            entity.HasKey(e => e.PesajeId).HasName("PK__Pesaje__03DC4C0867DE8B5B");

            entity.ToTable("Pesaje", "agro");

            entity.HasIndex(e => new { e.AnimalId, e.Fecha }, "IX_Pesaje_Animal_Fecha").IsDescending(false, true);

            entity.HasIndex(e => new { e.AnimalId, e.Fecha }, "UQ__Pesaje__492ABBA30C060F52").IsUnique();

            entity.Property(e => e.Observacion).HasMaxLength(300);
            entity.Property(e => e.PesoKg).HasColumnType("decimal(18, 3)");

            entity.HasOne(d => d.Animal).WithMany(p => p.Pesajes)
                .HasForeignKey(d => d.AnimalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Pesaje__AnimalId__17F790F9");
        });

        modelBuilder.Entity<Potrero>(entity =>
        {
            entity.HasKey(e => e.PotreroId).HasName("PK__Potrero__673CB670820876E7");

            entity.ToTable("Potrero", "agro");

            entity.HasIndex(e => new { e.FincaId, e.Nombre }, "UQ__Potrero__DBE0D5946768C80A").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Hectareas).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Nombre).HasMaxLength(120);

            entity.HasOne(d => d.Finca).WithMany(p => p.Potreros)
                .HasForeignKey(d => d.FincaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Potrero__FincaId__74AE54BC");
        });

        modelBuilder.Entity<Raza>(entity =>
        {
            entity.HasKey(e => e.RazaId).HasName("PK__Raza__39A423D838BEA012");

            entity.ToTable("Raza", "agro");

            entity.HasIndex(e => e.Nombre, "UQ__Raza__75E3EFCF0097E82F").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(120);
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.RolId).HasName("PK__Rol__F92302F1F2A4F3A0");

            entity.ToTable("Rol", "agro");

            entity.HasIndex(e => e.Nombre, "UQ__Rol__75E3EFCF9BA8E4C3").IsUnique();

            entity.Property(e => e.FechaCreacion).HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion).HasColumnType("datetime");
            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<RubroGasto>(entity =>
        {
            entity.HasKey(e => e.RubroGastoId).HasName("PK__RubroGas__7FAB05BF701B8B49");

            entity.ToTable("RubroGasto", "agro");

            entity.HasIndex(e => e.Nombre, "UQ__RubroGas__75E3EFCFFA349E2D").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(120);
        });

        modelBuilder.Entity<RubroIngreso>(entity =>
        {
            entity.HasKey(e => e.RubroIngresoId).HasName("PK__RubroIng__4F74782FA8C12E33");

            entity.ToTable("RubroIngreso", "agro");

            entity.HasIndex(e => e.Nombre, "UQ__RubroIng__75E3EFCF3BCD2A16").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(120);
        });

        modelBuilder.Entity<TipoMovimientoInventario>(entity =>
        {
            entity.HasKey(e => e.TipoId).HasName("PK__TipoMovi__97099EB7ADD6A42D");

            entity.ToTable("TipoMovimientoInventario", "agro");

            entity.HasIndex(e => e.Nombre, "UQ__TipoMovi__75E3EFCF87EB3142").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(80);
        });

        modelBuilder.Entity<TipoTratamiento>(entity =>
        {
            entity.HasKey(e => e.TipoTratId).HasName("PK__TipoTrat__627E84A29673BD4B");

            entity.ToTable("TipoTratamiento", "agro");

            entity.HasIndex(e => e.Nombre, "UQ__TipoTrat__75E3EFCF1A953E87").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(120);
        });

        modelBuilder.Entity<Tratamiento>(entity =>
        {
            entity.HasKey(e => e.TratamientoId).HasName("PK__Tratamie__6CFB22659AC62484");

            entity.ToTable("Tratamiento", "agro");

            entity.HasIndex(e => new { e.FincaId, e.Fecha }, "IX_Tratamiento_Finca_Fecha");

            entity.Property(e => e.Dosis).HasMaxLength(60);
            entity.Property(e => e.Fecha).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Observacion).HasMaxLength(300);
            entity.Property(e => e.Responsable).HasMaxLength(200);
            entity.Property(e => e.Via).HasMaxLength(40);

            entity.HasOne(d => d.Animal).WithMany(p => p.Tratamientos)
                .HasForeignKey(d => d.AnimalId)
                .HasConstraintName("FK__Tratamien__Anima__08B54D69");

            entity.HasOne(d => d.Finca).WithMany(p => p.Tratamientos)
                .HasForeignKey(d => d.FincaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tratamien__Finca__06CD04F7");

            entity.HasOne(d => d.Insumo).WithMany(p => p.Tratamientos)
                .HasForeignKey(d => d.InsumoId)
                .HasConstraintName("FK__Tratamien__Insum__0B91BA14");

            entity.HasOne(d => d.LoteAnimal).WithMany(p => p.Tratamientos)
                .HasForeignKey(d => d.LoteAnimalId)
                .HasConstraintName("FK__Tratamien__LoteA__09A971A2");

            entity.HasOne(d => d.Lote).WithMany(p => p.Tratamientos)
                .HasForeignKey(d => d.LoteId)
                .HasConstraintName("FK__Tratamien__LoteI__0C85DE4D");

            entity.HasOne(d => d.TipoTrat).WithMany(p => p.Tratamientos)
                .HasForeignKey(d => d.TipoTratId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tratamien__TipoT__07C12930");
        });

        modelBuilder.Entity<UnidadMedidum>(entity =>
        {
            entity.HasKey(e => e.UnidadId).HasName("PK__UnidadMe__C6F324D6B55F7B5A");

            entity.ToTable("UnidadMedida", "agro");

            entity.HasIndex(e => e.Codigo, "UQ__UnidadMe__06370DAC4FE9848A").IsUnique();

            entity.Property(e => e.Codigo).HasMaxLength(30);
            entity.Property(e => e.Nombre).HasMaxLength(80);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__1788CC4CCC96A2F8");

            entity.ToTable("User", "agro");

            entity.HasIndex(e => e.Email, "UQ__User__A9D1053427CA21FA").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(200);

            entity.HasOne(d => d.Rol).WithMany(p => p.Users)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__User__RolId__403A8C7D");

            
        });

        modelBuilder.Entity<UserFinca>(entity =>
        {
            entity.ToTable("UserFinca", "agro");

            entity.HasKey(e => e.UserFincaId);

            entity.Property(e => e.AspNetUserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.HasOne(d => d.Finca)
                .WithMany(p => p.UserFincas)
                .HasForeignKey(d => d.FincaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.AspNetUserId, e.FincaId })
                .IsUnique();
        });



        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
