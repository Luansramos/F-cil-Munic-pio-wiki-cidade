using System.Text.Json;
using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Data;
using FacilMunicipio.Api.Domain;
using FacilMunicipio.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FacilMunicipio.Api.Services;

public class PublicationService(
    AppDbContext db,
    IKnowledgeGraph graph,
    CurrentUser user,
    SinkGuard guard)
{
    public async Task<Versao> PublishAsync(Guid id, CancellationToken ct)
    {
        var municipioId = await db.Rascunhos
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => r.MunicipioId)
            .SingleOrDefaultAsync(ct)
            ?? throw new ApiException(404, "Rascunho não encontrado.");

        user.RequireMunicipio(municipioId);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // O bloqueio impede revisão e publicação simultâneas no mesmo município.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({municipioId}))", ct);

        var rascunho = await db.Rascunhos.SingleAsync(r => r.Id == id, ct);

        if (!rascunho.Aprovado)
            throw new ApiException(409, "Revise e aprove o rascunho antes de publicar.");

        // A mesma revisão nunca deve criar uma segunda versão.
        var existente = await db.Versoes
            .AsNoTracking()
            .SingleOrDefaultAsync(v => v.Id == rascunho.RevisaoId, ct);

        if (existente is not null)
            return existente;

        var municipio = await db.Municipios.SingleAsync(m => m.Id == municipioId, ct);
        var fonte = await db.Fontes.SingleAsync(
            f => f.Id == rascunho.FonteId && f.MunicipioId == municipioId, ct);

        var servico = JsonSerializer.Deserialize<ServicoRevisado>(rascunho.ConteudoJson)!;

        // Sink: publicação no grafo. Rascunhos antigos ou alterados fora da API também são verificados.
        // Falha aqui encerra a transação antes de qualquer gravação.
        await guard.RequireCleanAsync(municipioId, SinkDestino.Grafo,
            SinkGuard.Fields(servico).Concat([("fonte.titulo", (string?)fonte.Titulo), ("fonte.url", fonte.Url)]));

        // Preserva os outros serviços que já estão publicados.
        var conhecimentos = municipio.VersaoAtivaId is Guid versaoAnterior
            ? (await graph.ReadAsync(municipioId, versaoAnterior, ct))
                .Where(item => item.Servico.Id != servico.Id)
                .ToList()
            : new List<Conhecimento>();

        conhecimentos.Add(new Conhecimento(
            servico,
            new FonteDto(fonte.Titulo, fonte.Url, fonte.DataConsulta.ToString("yyyy-MM-dd"))));

        var versao = new Versao
        {
            Id = rascunho.RevisaoId,
            MunicipioId = municipioId,
            RascunhoId = id
        };

        db.Versoes.Add(versao);
        await db.SaveChangesAsync(ct);

        await graph.WriteAsync(municipioId, versao.Id, conhecimentos, ct);

        versao.Publicar();
        municipio.VersaoAtivaId = versao.Id;

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        // Se o PostgreSQL não concluir a transação, o grafo pode guardar
        // uma versão órfã, mas ela não será exibida na consulta pública.
        return versao;
    }
}
