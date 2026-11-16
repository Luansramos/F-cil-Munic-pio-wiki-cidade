using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Data;
using FacilMunicipio.Api.Domain;
using FacilMunicipio.Api.Security;
using Microsoft.EntityFrameworkCore;
namespace FacilMunicipio.Api.Services;
public class ConversationService(AppDbContext db, IKnowledgeGraph graph, ILlmWiki llm, SinkGuard guard)
{
    public async Task<RespostaConversa> AskAsync(string id, EnvioPergunta request, CancellationToken ct)
    {
        // Os eventos da operação são gravados uma única vez, mesmo em retorno antecipado ou erro (máx. 2 s;
        // pior caso: 12 s de orçamento + 2 s de auditoria, abaixo do timeout de 15 s do app).
        try { return await AskCoreAsync(id, request, ct); }
        finally { await guard.FlushAsync(ct); }
    }

    private async Task<RespostaConversa> AskCoreAsync(string id, EnvioPergunta request, CancellationToken ct)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
        budget.CancelAfter(TimeSpan.FromSeconds(12));
        var token = budget.Token;
        var municipio = await db.Municipios.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, token)
            ?? throw new ApiException(404, "Município não encontrado.");
        // Source → marcação → tratamento: a entrada é analisada ANTES dos retornos antecipados abaixo
        // (e só para município válido). Política "Conversa": remove PII e continua com o texto seguro.
        var question = guard.Sanitize(id, request.Pergunta, "conversa.pergunta", SinkDestino.Llm);
        // Canal oficial do município também é saída pública: só sai se passar pela verificação.
        var channel = guard.Verify(id, SinkDestino.RespostaPublica, ("resposta.canal", municipio.CanalOficialUrl)) ? municipio.CanalOficialUrl : null;
        var history = new List<TrocaResumida>();
        foreach (var (item, i) in (request.Historico ?? []).Select((x, i) => (x, i)))
            history.Add(new(guard.Sanitize(id, item.Pergunta, $"conversa.historico[{i}].pergunta", SinkDestino.Llm),
                guard.Sanitize(id, item.Resposta, $"conversa.historico[{i}].resposta", SinkDestino.Llm)));
        if (municipio.VersaoAtivaId is not Guid version) return RespostaConversa.Insuficiente(channel);
        var knowledge = await graph.ReadAsync(id, version, token);
        if (knowledge.Count == 0) return RespostaConversa.Insuficiente(channel);
        // Sink 1: chamada à LLM. Só segue texto já tratado; qualquer resíduo marcado bloqueia a chamada.
        // Inclui o conhecimento publicado inteiro que será enviado como contexto, não só pergunta e histórico.
        (string, string?)[] llmFields =
        [
            ("conversa.pergunta", question),
            .. SinkGuard.Fields(knowledge),
            .. history.SelectMany((h, i) => new (string, string?)[] { ($"conversa.historico[{i}].pergunta", h.Pergunta), ($"conversa.historico[{i}].resposta", h.Resposta) })
        ];
        if (!guard.Verify(id, SinkDestino.Llm, llmFields))
            return RespostaConversa.Bloqueada(channel);
        // Pilot has few services: all published evidence in this city/version is supplied.
        // History is untrusted conversational context, never evidence or another city's data source.
        var result = await llm.ResponderAsync(question, history.ToArray(), knowledge, token);
        if (!result.InformacaoSuficiente) return RespostaConversa.Insuficiente(channel);
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
        var officialChannel = serviceChannels.Length == 1 ? serviceChannels[0] : channel;
        var response = new RespostaConversa(result.Resposta, ids, sources, true, officialChannel);
        // Sink 2: resposta pública. A LLM pode gerar PII que não estava na entrada; todos os campos são verificados.
        if (!guard.Verify(id, SinkDestino.RespostaPublica, ResponseFields(response)))
            return RespostaConversa.Bloqueada(channel);
        return response;
    }

    private static (string, string?)[] ResponseFields(RespostaConversa r) =>
    [
        ("llm.resposta", r.Resposta), ("resposta.canal", r.CanalOficialUrl),
        .. r.ServicosRelacionados.Select((s, i) => ($"resposta.servicos[{i}]", (string?)s)),
        .. r.Fontes.SelectMany((f, i) => new (string, string?)[] { ($"resposta.fontes[{i}].titulo", f.Titulo), ($"resposta.fontes[{i}].url", f.Url) })
    ];
}
