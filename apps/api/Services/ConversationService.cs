using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Data;
using FacilMunicipio.Api.Security;
using Microsoft.EntityFrameworkCore;
namespace FacilMunicipio.Api.Services;
public class ConversationService(AppDbContext db, IKnowledgeGraph graph, ILlmWiki llm)
{
    public async Task<RespostaConversa> AskAsync(string id, EnvioPergunta request, CancellationToken ct)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
        budget.CancelAfter(TimeSpan.FromSeconds(12));
        var token = budget.Token;
        var municipio = await db.Municipios.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, token)
            ?? throw new ApiException(404, "Município não encontrado.");
        if (municipio.VersaoAtivaId is not Guid version) return RespostaConversa.Insuficiente(municipio.CanalOficialUrl);
        var knowledge = await graph.ReadAsync(id, version, token);
        if (knowledge.Count == 0) return RespostaConversa.Insuficiente(municipio.CanalOficialUrl);
        // Pilot has few services: all published evidence in this city/version is supplied.
        // History is untrusted conversational context, never evidence or another city's data source.
        var result = await llm.ResponderAsync(PrivacyFilter.Clean(request.Pergunta),
            PrivacyFilter.Clean(request.Historico), knowledge, token);
        if (!result.InformacaoSuficiente) return RespostaConversa.Insuficiente(municipio.CanalOficialUrl);
        var ids = result.ServicoIds?.Distinct().ToArray() ?? [];
        if (ids.Length == 0 || ids.Any(x => knowledge.All(k => k.Servico.Id != x)) || string.IsNullOrWhiteSpace(result.Resposta))
            throw new ApiException(503, "O serviço de resposta retornou conteúdo inválido.");
        var sources = knowledge.Where(k => ids.Contains(k.Servico.Id)).Select(k => k.Fonte).Distinct().ToArray();
        // Sources and channel URLs come from approved graph data, never from the LLM.
        // If several services provide different channels, use the city-level official URL
        // only when configured rather than arbitrarily choosing one service.
        var serviceChannels = knowledge.Where(k => ids.Contains(k.Servico.Id))
            .Select(k => k.Servico.CanalUrl).Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var officialChannel = serviceChannels.Length == 1 ? serviceChannels[0] : municipio.CanalOficialUrl;
        return new(PrivacyFilter.Clean(result.Resposta), ids, sources, true, officialChannel);
    }
}
