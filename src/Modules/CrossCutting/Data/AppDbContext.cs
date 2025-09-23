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
       });
    }
}
