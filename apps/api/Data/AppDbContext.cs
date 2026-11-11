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
    }
}
