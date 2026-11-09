using System.Text.Json;
using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Data;
using FacilMunicipio.Api.Domain;
using FacilMunicipio.Api.Security;
using Microsoft.EntityFrameworkCore;
namespace FacilMunicipio.Api.Services;
public class PublicationService(AppDbContext db, IKnowledgeGraph graph, CurrentUser user)
{
    public async Task<Versao> PublishAsync(Guid id, CancellationToken ct)
    {
        var city = await db.Rascunhos.AsNoTracking().Where(x => x.Id == id).Select(x => x.MunicipioId).SingleOrDefaultAsync(ct)
            ?? throw new ApiException(404, "Rascunho não encontrado.");
        user.RequireMunicipio(city);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Cross-process serialization for publication AND revision. Collision only over-serializes.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({city}))", ct);
        var draft = await db.Rascunhos.SingleAsync(x => x.Id == id, ct);
        if (!draft.Aprovado) throw new ApiException(409, "Revise e aprove o rascunho antes de publicar.");
        var existing = await db.Versoes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == draft.RevisaoId, ct);
        if (existing is not null) return existing; // Never reactivate an old publication on retry.
        var municipio = await db.Municipios.SingleAsync(x => x.Id == city, ct);
        var source = await db.Fontes.SingleAsync(x => x.Id == draft.FonteId && x.MunicipioId == city, ct);
        var service = JsonSerializer.Deserialize<ServicoRevisado>(draft.ConteudoJson)!;
        var snapshot = municipio.VersaoAtivaId is Guid previous
            ? (await graph.ReadAsync(city, previous, ct)).Where(x => x.Servico.Id != service.Id).ToList()
            : new List<Conhecimento>();
        snapshot.Add(new(service, new(source.Titulo, source.Url, source.DataConsulta.ToString("yyyy-MM-dd"))));
        var version = new Versao { Id = draft.RevisaoId, MunicipioId = city, RascunhoId = id };
        db.Versoes.Add(version);
        await db.SaveChangesAsync(ct);
        await graph.WriteAsync(city, version.Id, snapshot, ct);
        version.Publicar();
        municipio.VersaoAtivaId = version.Id;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        // On failure DB rolls back; immutable graph version may remain orphaned but is never read.
        // Retrying uses the same revision id and MERGE to safely overwrite that unpublished snapshot.
        return version;
    }
}
