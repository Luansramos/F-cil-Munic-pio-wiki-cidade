using FacilMunicipio.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FacilMunicipio.Api.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Municipio> Municipios => Set<Municipio>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Fonte> Fontes => Set<Fonte>();
    public DbSet<Rascunho> Rascunhos => Set<Rascunho>();
    public DbSet<Versao> Versoes => Set<Versao>();
    public DbSet<EventoSeguranca> EventosSeguranca => Set<EventoSeguranca>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Municipio>().HasKey(x => x.Id);
        b.Entity<Municipio>().Property(x => x.Id).HasMaxLength(60);
        b.Entity<Usuario>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Usuario>().HasOne<Municipio>().WithMany().HasForeignKey(x => x.MunicipioId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Fonte>().HasOne<Municipio>().WithMany().HasForeignKey(x => x.MunicipioId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Rascunho>().HasOne<Fonte>().WithMany().HasForeignKey(x => x.FonteId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Rascunho>().HasOne<Municipio>().WithMany().HasForeignKey(x => x.MunicipioId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Rascunho>().Property(x => x.RevisaoId).IsConcurrencyToken();
        b.Entity<Versao>().HasOne<Municipio>().WithMany().HasForeignKey(x => x.MunicipioId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Versao>().HasOne<Rascunho>().WithMany().HasForeignKey(x => x.RascunhoId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Versao>().HasIndex(x => new { x.MunicipioId, x.CriadaEm });
        var e = b.Entity<EventoSeguranca>();
        e.HasOne<Municipio>().WithMany().HasForeignKey(x => x.MunicipioId).OnDelete(DeleteBehavior.Restrict);
        e.Property(x => x.Categoria).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.Destino).HasConversion<string>().HasMaxLength(30);
        e.Property(x => x.Acao).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.Gravidade).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.TraducaoEstado).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.Origem).HasMaxLength(80);
        e.Property(x => x.Codigo).HasMaxLength(40);
        e.Property(x => x.TraducaoTexto).HasMaxLength(500);
        e.HasIndex(x => new { x.MunicipioId, x.OcorridoEm });
        e.HasIndex(x => new { x.TraducaoEstado, x.OcorridoEm });
    }
}
