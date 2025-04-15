using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace LibEF.Models;

public partial class ProjectoContext : DbContext
{
    public ProjectoContext()
    {
    }

    public ProjectoContext(DbContextOptions<ProjectoContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Autor> Autors { get; set; }

    public virtual DbSet<Genero> Generos { get; set; }

    public virtual DbSet<History> Histories { get; set; }

    public virtual DbSet<ImageReference> ImageReferences { get; set; }

    public virtual DbSet<Leitor> Leitors { get; set; }

    public virtual DbSet<Nucleo> Nucleos { get; set; }

    public virtual DbSet<NucleoObra> NucleoObras { get; set; }

    public virtual DbSet<Obra> Obras { get; set; }

    public virtual DbSet<Requisicao> Requisicaos { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseSqlServer("Server=localhost;Database=Projecto;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Autor>(entity =>
        {
            entity.HasKey(e => e.PkAutor).HasName("PK__Autor__28A206020E28DBF7");

            entity.ToTable("Autor");

            entity.Property(e => e.PkAutor).HasColumnName("pk_autor");
            entity.Property(e => e.NomeAutor)
                .HasMaxLength(50)
                .HasColumnName("nome_autor");

            entity.HasMany(d => d.PkObras).WithMany(p => p.PkAutors)
                .UsingEntity<Dictionary<string, object>>(
                    "AutorObra",
                    r => r.HasOne<Obra>().WithMany()
                        .HasForeignKey("PkObra")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__AutorObra__pk_ob__35BCFE0A"),
                    l => l.HasOne<Autor>().WithMany()
                        .HasForeignKey("PkAutor")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__AutorObra__pk_au__34C8D9D1"),
                    j =>
                    {
                        j.HasKey("PkAutor", "PkObra").HasName("PK__AutorObr__1D468FD2CFA071A4");
                        j.ToTable("AutorObra");
                        j.IndexerProperty<int>("PkAutor").HasColumnName("pk_autor");
                        j.IndexerProperty<int>("PkObra").HasColumnName("pk_obra");
                    });
        });

        modelBuilder.Entity<Genero>(entity =>
        {
            entity.HasKey(e => e.PkGenero).HasName("PK__Genero__3DA5A65C20BB52DF");

            entity.ToTable("Genero");

            entity.Property(e => e.PkGenero).HasColumnName("pk_genero");
            entity.Property(e => e.NomeGenero)
                .HasMaxLength(50)
                .HasColumnName("nome_genero");

            entity.HasMany(d => d.PkObras).WithMany(p => p.PkGeneros)
                .UsingEntity<Dictionary<string, object>>(
                    "GeneroObra",
                    r => r.HasOne<Obra>().WithMany()
                        .HasForeignKey("PkObra")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__GeneroObr__pk_ob__300424B4"),
                    l => l.HasOne<Genero>().WithMany()
                        .HasForeignKey("PkGenero")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__GeneroObr__pk_ge__2F10007B"),
                    j =>
                    {
                        j.HasKey("PkGenero", "PkObra").HasName("PK__GeneroOb__08412F8CF700621D");
                        j.ToTable("GeneroObra");
                        j.IndexerProperty<int>("PkGenero").HasColumnName("pk_genero");
                        j.IndexerProperty<int>("PkObra").HasColumnName("pk_obra");
                    });
        });

        modelBuilder.Entity<History>(entity =>
        {
            entity.HasKey(e => e.PkLog).HasName("PK__History__DCFB38B364FE5AF3");

            entity.ToTable("History");

            entity.Property(e => e.PkLog).HasColumnName("pk_log");
            entity.Property(e => e.DataDevolucao).HasColumnName("data_devolucao");
            entity.Property(e => e.DataRequisicao).HasColumnName("data_requisicao");
            entity.Property(e => e.IdLeitor).HasColumnName("id_leitor");
            entity.Property(e => e.IdObra).HasColumnName("id_obra");
            entity.Property(e => e.NomeLeitor)
                .HasMaxLength(50)
                .HasColumnName("nome_leitor");
            entity.Property(e => e.NomeObra)
                .HasMaxLength(50)
                .HasColumnName("nome_obra");
            entity.Property(e => e.Nucleo)
                .HasMaxLength(50)
                .HasColumnName("nucleo");
        });

        modelBuilder.Entity<ImageReference>(entity =>
        {
            entity.HasKey(e => e.PkImage).HasName("PK__ImageRef__4B9CF7FAFC955F4E");

            entity.Property(e => e.PkImage).HasColumnName("pk_image");
            entity.Property(e => e.ImageData).HasColumnName("image_data");
            entity.Property(e => e.ImageName)
                .HasMaxLength(50)
                .HasColumnName("image_name");
            entity.Property(e => e.ImagePath)
                .HasMaxLength(255)
                .HasColumnName("image_path");
        });

        modelBuilder.Entity<Leitor>(entity =>
        {
            entity.HasKey(e => e.PkLeitor).HasName("PK__Leitor__08D1482B0A85CC42");

            entity.ToTable("Leitor");

            entity.Property(e => e.PkLeitor).HasColumnName("pk_leitor");
            entity.Property(e => e.DataInscricao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("data_inscricao");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("email");
            entity.Property(e => e.Morada)
                .HasMaxLength(50)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("morada");
            entity.Property(e => e.NomeLeitor)
                .HasMaxLength(50)
                .HasColumnName("nome_leitor");
            entity.Property(e => e.Stat)
                .HasMaxLength(50)
                .HasDefaultValue("active")
                .HasColumnName("stat");
            entity.Property(e => e.Telefone)
                .HasMaxLength(20)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("telefone");
            entity.Property(e => e.UserPassword)
                .HasMaxLength(50)
                .HasColumnName("user_password");
            entity.Property(e => e.UserRole)
                .HasMaxLength(50)
                .HasDefaultValue("USER")
                .HasColumnName("user_role");
        });

        modelBuilder.Entity<Nucleo>(entity =>
        {
            entity.HasKey(e => e.PkNucleo).HasName("PK__Nucleo__1E38ABAFEF9CD7F7");

            entity.ToTable("Nucleo");

            entity.Property(e => e.PkNucleo).HasColumnName("pk_nucleo");
            entity.Property(e => e.Morada)
                .HasMaxLength(50)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("morada");
            entity.Property(e => e.NomeNucleo)
                .HasMaxLength(50)
                .HasColumnName("nome_nucleo");
            entity.Property(e => e.Telefone)
                .HasMaxLength(20)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("telefone");
        });

        modelBuilder.Entity<NucleoObra>(entity =>
        {
            entity.HasKey(e => new { e.PkNucleo, e.PkObra }).HasName("PK__NucleoOb__2BDC227F9991F691");

            entity.ToTable("NucleoObra");

            entity.Property(e => e.PkNucleo).HasColumnName("pk_nucleo");
            entity.Property(e => e.PkObra).HasColumnName("pk_obra");
            entity.Property(e => e.Quantidade).HasColumnName("quantidade");

            entity.HasOne(d => d.PkNucleoNavigation).WithMany(p => p.NucleoObras)
                .HasForeignKey(d => d.PkNucleo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NucleoObr__pk_nu__44FF419A");

            entity.HasOne(d => d.PkObraNavigation).WithMany(p => p.NucleoObras)
                .HasForeignKey(d => d.PkObra)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NucleoObr__pk_ob__45F365D3");
        });

        modelBuilder.Entity<Obra>(entity =>
        {
            entity.HasKey(e => e.PkObra).HasName("PK__Obra__5E489D024BB11855");

            entity.ToTable("Obra");

            entity.Property(e => e.PkObra).HasColumnName("pk_obra");
            entity.Property(e => e.Ano).HasColumnName("ano");
            entity.Property(e => e.Editora)
                .HasMaxLength(50)
                .HasColumnName("editora");
            entity.Property(e => e.FkImagem).HasColumnName("fk_imagem");
            entity.Property(e => e.Isbn)
                .HasMaxLength(50)
                .HasColumnName("ISBN");
            entity.Property(e => e.NomeObra)
                .HasMaxLength(50)
                .HasColumnName("nome_obra");

            entity.HasOne(d => d.FkImagemNavigation).WithMany(p => p.Obras)
                .HasForeignKey(d => d.FkImagem)
                .HasConstraintName("FK__Obra__fk_imagem__2A4B4B5E");
        });

        modelBuilder.Entity<Requisicao>(entity =>
        {
            entity.HasKey(e => new { e.PkLeitor, e.PkObra, e.PkNucleo }).HasName("PK__Requisic__932BF9500D5E3BF8");

            entity.ToTable("Requisicao");

            entity.Property(e => e.PkLeitor).HasColumnName("pk_leitor");
            entity.Property(e => e.PkObra).HasColumnName("pk_obra");
            entity.Property(e => e.PkNucleo).HasColumnName("pk_nucleo");
            entity.Property(e => e.AlreadySuspend)
                .HasDefaultValue(false)
                .HasColumnName("already_suspend");
            entity.Property(e => e.DataDevolucao)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("data_devolucao");
            entity.Property(e => e.DataLevantamento).HasColumnName("data_levantamento");
            entity.Property(e => e.Stat)
                .HasMaxLength(50)
                .HasDefaultValue("borrowed")
                .HasColumnName("stat");

            entity.HasOne(d => d.PkLeitorNavigation).WithMany(p => p.Requisicaos)
                .HasForeignKey(d => d.PkLeitor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Requisica__pk_le__4BAC3F29");

            entity.HasOne(d => d.PkObraNavigation).WithMany(p => p.Requisicaos)
                .HasForeignKey(d => d.PkObra)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Requisica__pk_ob__4CA06362");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
