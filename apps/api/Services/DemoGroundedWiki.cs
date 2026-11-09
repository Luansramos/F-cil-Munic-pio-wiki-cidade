using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FacilMunicipio.Api.Contracts;

namespace FacilMunicipio.Api.Services;

/// <summary>
/// Explicit demo-only deterministic responder; NEVER presented as an LLM.
/// Produces text exclusively from the published graph snapshot supplied by the API.
/// No question or history is written to disk or logged.
/// </summary>
public sealed class DemoGroundedWiki : ILlmWiki
{
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "como", "posso", "para", "quero", "preciso", "gostaria", "qual", "quais",
        "voce", "voces", "esta", "este", "sobre", "minha", "meu", "uma", "uns",
        "por", "favor", "servico", "cidade", "municipio"
    };

    // A question about a detail not recorded in the reviewed source must not receive
    // a misleading generic answer. This deliberately favors abstaining.
    private static readonly HashSet<string> Qualifiers = new(StringComparer.Ordinal)
    {
        "sabado", "domingo", "feriado", "hoje", "amanha", "horario",
        "horarios", "quando", "prazo", "prazos", "taxa", "custo", "preco",
        "valor", "eletronico", "eletronicos", "endereco", "documentos",
        "agendamento", "agendar"
    };

    public Task<ServicoRevisado> ExtrairAsync(string texto, CancellationToken ct)
        => throw new LlmUnavailableException(); // Manual drafts remain available.

    public Task<ResultadoLlm> ResponderAsync(string pergunta, TrocaResumida[] historico,
        IReadOnlyList<Conhecimento> contexto, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var tokens = Tokens(pergunta).Where(t => !StopWords.Contains(t)).Distinct().ToArray();
        if (tokens.Length == 0) return Task.FromResult(Insufficient());

        var best = contexto.Select(item => new
        {
            Item = item,
            Score = tokens.Count(t => Tokens(string.Join(" ",
                item.Servico.Nome, item.Servico.Descricao, string.Join(" ", item.Servico.Termos))).Contains(t)),
            EvidenceTokens = Tokens(string.Join(" ", item.Servico.Nome,
                item.Servico.Descricao, item.Servico.Orgao ?? "",
                string.Join(" ", item.Servico.Etapas), string.Join(" ", item.Servico.Requisitos),
                string.Join(" ", item.Servico.Termos))).ToHashSet()
        }).Where(candidate => candidate.Score > 0)
          .OrderByDescending(candidate => candidate.Score)
          .ThenBy(candidate => candidate.Item.Servico.Id)
          .FirstOrDefault();

        if (best is null || tokens.Any(t => Qualifiers.Contains(t) && !best.EvidenceTokens.Contains(t)))
            return Task.FromResult(Insufficient());

        var service = best.Item.Servico;
        var lines = new List<string> { service.Nome + ": " + service.Descricao };
        if (service.Etapas.Length > 0)
            lines.Add("Etapas:\n" + string.Join("\n", service.Etapas.Select((s, i) => $"{i + 1}. {s}")));
        if (service.Requisitos.Length > 0)
            lines.Add("Requisitos:\n" + string.Join("\n", service.Requisitos.Select(s => "- " + s)));
        if (service.CanalUrl is not null)
            lines.Add("Canal informado na fonte revisada: " + service.CanalUrl);

        return Task.FromResult(new ResultadoLlm(string.Join("\n\n", lines), [service.Id], true));
    }

    private static ResultadoLlm Insufficient() => new("", [], false);

    private static HashSet<string> Tokens(string text)
    {
        var normalized = new string(text.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray()).ToLowerInvariant();
        return Regex.Matches(normalized, "[a-z0-9]{3,}")
            .Cast<Match>().Select(m => m.Value).ToHashSet(StringComparer.Ordinal);
    }
}
