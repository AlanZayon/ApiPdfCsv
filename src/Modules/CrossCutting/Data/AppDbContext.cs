// CrossCutting/Data/AppDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ApiPdfCsv.Modules.Authentication.Domain.Entities;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;

namespace ApiPdfCsv.CrossCutting.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<CodigoConta> CodigoConta { get; set; }
    public DbSet<Imposto> Imposto { get; set; }
    public DbSet<TermoEspecial> TermoEspecial { get; set; }
    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<ApiPdfCsv.Shared.Processing.UploadJob> UploadJobs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Imposto>(entity =>
        {
            entity.ToTable("Imposto");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nome).HasColumnName("nome");
            entity.Property(e => e.CodigoDebitoId).HasColumnName("codigodebitoid");
            entity.Property(e => e.CodigoCreditoId).HasColumnName("codigocreditoid");
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.Property(e => e.ClienteId).HasColumnName("clienteid");

            entity.HasOne(i => i.Cliente)
                .WithMany()
                .HasForeignKey(i => i.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.CodigoDebito)
                .WithMany()
                .HasForeignKey(i => i.CodigoDebitoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.CodigoCredito)
                .WithMany()
                .HasForeignKey(i => i.CodigoCreditoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CodigoConta>(entity =>
        {
            entity.ToTable("CodigoConta");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nome).HasColumnName("nome");
            entity.Property(e => e.Codigo).HasColumnName("codigo");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.UserId).HasColumnName("userid");
        });

        builder.Entity<TermoEspecial>(entity =>
       {
           entity.ToTable("TermoEspecial");
           entity.HasKey(e => e.Id);

           entity.Property(e => e.Id)
               .HasColumnName("id")
               .IsRequired();

           entity.Property(e => e.Termo)
               .HasColumnName("termo")
               .IsRequired();

           entity.Property(e => e.UserId)
               .HasColumnName("userId")
               .IsRequired();

           entity.Property(e => e.CodigoDebito)
               .HasColumnName("codigodebito");

           entity.Property(e => e.CodigoCredito)
               .HasColumnName("codigocredito");

           entity.Property(e => e.CodigoBanco)
               .HasColumnName("codigoBanco");

           entity.Property(e => e.CNPJ)
               .HasColumnName("CNPJ");

           entity.Property(e => e.TipoValor)
            .HasColumnName("tipovalor");

           entity.HasIndex(e => new { e.UserId, e.CNPJ, e.CodigoBanco })
               .HasDatabaseName("IX_TermoEspecial_User_Cnpj_Banco");

           entity.HasIndex(e => new { e.UserId, e.CNPJ, e.CodigoBanco, e.Termo, e.TipoValor })
               .HasDatabaseName("IX_TermoEspecial_Lookup");
       });

        builder.Entity<ApiPdfCsv.Shared.Processing.UploadJob>(entity =>
        {
            entity.ToTable("UploadJobs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(32);
            entity.Property(e => e.UserId).HasMaxLength(450).IsRequired();
            entity.Property(e => e.SessionId).HasMaxLength(256).IsRequired();
            entity.Property(e => e.State).HasConversion<string>().HasMaxLength(32);
            entity.Property(e => e.JobKind).HasMaxLength(32).IsRequired();
            entity.Property(e => e.FileType).HasMaxLength(16);
            entity.Property(e => e.InputFileName).HasMaxLength(256);
            entity.Property(e => e.OutputFile).HasMaxLength(256);
            entity.Property(e => e.Message).HasMaxLength(2000);
            entity.Property(e => e.ResultJson).HasColumnType("jsonb");
            entity.Property(e => e.MetadataJson).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.UserId, e.Id }).HasDatabaseName("IX_UploadJobs_User_Job");
        });

        builder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Clientes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("userid").HasMaxLength(450).IsRequired();
            entity.Property(e => e.Cnpj).HasColumnName("cnpj").HasMaxLength(14).IsRequired();
            entity.Property(e => e.RazaoSocial).HasColumnName("razaosocial").HasMaxLength(256).IsRequired();
            entity.Property(e => e.CodigoBancoPadrao).HasColumnName("codigobancopadrao");
            entity.Property(e => e.Ativo).HasColumnName("ativo").HasDefaultValue(true);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("createdatutc");

            entity.HasIndex(e => new { e.UserId, e.Cnpj })
                .IsUnique()
                .HasDatabaseName("IX_Clientes_User_Cnpj");
        });
    }
}
