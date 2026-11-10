using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FacilMunicipio.Api.Contracts;

namespace FacilMunicipio.Api.Services;

/// <summary>
/// Respondedor local usado apenas na demonstração. Não substitui a LLM.
/// A pergunta e o histórico servem para identificar um serviço publicado;
/// a resposta usa somente os dados revisados do grafo.
/// </summary>
public sealed class DemoGroundedWiki : ILlmWiki
{
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "como", "posso", "para", "quero", "preciso", "gostaria", "qual", "quais",
        "voce", "voces", "esta", "este", "sobre", "minha", "meu", "uma", "uns",
        "por", "favor", "servico", "cidade", "municipio", "a", "o", "os", "as",
        "de", "da", "do", "das", "dos", "na", "no", "nas", "nos", "em", "e",
        "se", "for", "ser", "tem", "que", "pode", "tambem"
    };

    // Detalhes que precisam constar na descrição, etapa ou requisito revisado.
    // Um termo de busca não é evidência de horário, prazo ou valor.
    private static readonly HashSet<string> Qualifiers = new(StringComparer.Ordinal)
    {
        "sabado", "domingo", "feriado", "hoje", "amanha", "horario",
        "horarios", "quando", "prazo", "prazos", "taxa", "custo", "preco",
        "valor", "eletronico", "eletronicos", "endereco", "documentos",
        "agendamento", "agendar"
    };

    private sealed record Candidate(
        Conhecimento Knowledge,
        HashSet<string> SearchTerms,
        HashSet<string> EvidenceTerms);

    public Task<ServicoRevisado> ExtrairAsync(string texto, CancellationToken ct)
        => throw new LlmUnavailableException();

    public Task<ResultadoLlm> ResponderAsync(
        string pergunta,
        TrocaResumida[] historico,
        IReadOnlyList<Conhecimento> contexto,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var candidates = contexto.Select(item => new Candidate(
            item,
            Tokens(string.Join(" ", item.Servico.Nome, item.Servico.Descricao,
                string.Join(" ", item.Servico.Termos))),
            Tokens(string.Join(" ", item.Servico.Nome, item.Servico.Descricao,
                item.Servico.Orgao ?? "", string.Join(" ", item.Servico.Etapas),
                string.Join(" ", item.Servico.Requisitos),
                item.Servico.CanalUrl ?? "")))).ToArray();

        var currentTokens = RelevantTokens(pergunta);
        var selected = BestMatch(currentTokens, candidates);

        // "E no domingo?" pode se referir à pergunta anterior.
        // Só usamos o histórico em uma pergunta curta sobre um detalhe do serviço.
        // Uma pergunta com outro assunto explícito não herda o serviço anterior.
        if (selected is null
            && currentTokens.Length > 0
            && currentTokens.All(token => Qualifiers.Contains(token)))
        {
            foreach (var exchange in (historico ?? []).TakeLast(3).Reverse())
            {
                ct.ThrowIfCancellationRequested();
                selected = BestMatch(RelevantTokens(exchange.Pergunta), candidates)
                    ?? BestMatch(RelevantTokens(exchange.Resposta), candidates);
                if (selected is not null)
                    break;
            }
        }

        if (selected is null ||
            currentTokens.Any(token => Qualifiers.Contains(token)
                && !selected.EvidenceTerms.Contains(token)))
        {
            return Task.FromResult(Insufficient());
        }

        var service = selected.Knowledge.Servico;
        var lines = new List<string> { service.Nome + ": " + service.Descricao };

        if (service.Etapas.Length > 0)
            lines.Add("Etapas:\n" + string.Join("\n",
                service.Etapas.Select((step, index) => $"{index + 1}. {step}")));

        if (service.Requisitos.Length > 0)
            lines.Add("Requisitos:\n" + string.Join("\n",
                service.Requisitos.Select((item, index) => $"{index + 1}. {item}")));

        if (!string.IsNullOrWhiteSpace(service.CanalUrl))
            lines.Add("Canal oficial informado na fonte revisada: " + service.CanalUrl);

        return Task.FromResult(new ResultadoLlm(
            string.Join("\n\n", lines), [service.Id], true));
    }

    private static Candidate? BestMatch(
        string[] tokens, IReadOnlyList<Candidate> candidates)
    {
        if (tokens.Length == 0)
            return null;

        return candidates
            .Select(candidate => new
            {
                Candidate = candidate,
                Score = tokens.Count(candidate.SearchTerms.Contains)
            })
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Candidate.Knowledge.Servico.Id, StringComparer.Ordinal)
            .Select(match => match.Candidate)
            .FirstOrDefault();
    }

    private static string[] RelevantTokens(string text) =>
        Tokens(text).Where(token => !StopWords.Contains(token)).ToArray();

    private static ResultadoLlm Insufficient() => new("", [], false);

    private static HashSet<string> Tokens(string text)
    {
        var normalized = new string(text.Normalize(NormalizationForm.FormD)
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character)
                != UnicodeCategory.NonSpacingMark)
            .ToArray()).ToLowerInvariant();

        return Regex.Matches(normalized, "[a-z0-9]{3,}")
            .Cast<Match>()
            .Select(match => match.Value)
            .ToHashSet(StringComparer.Ordinal);
    }
}
