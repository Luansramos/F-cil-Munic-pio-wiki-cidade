using System.Text;
using System.Text.RegularExpressions;
using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Domain;
namespace FacilMunicipio.Api.Security;

/// <summary>Ocorrência de PII: categoria e caminho do campo. Nunca guarda o valor.</summary>
public sealed record PiiFinding(PiiCategory Category, string Path);

/// <summary>
/// Valor detectado, mantido apenas em memória durante a requisição para rastrear a "contaminação"
/// (taint) por transformações. Não é serializável de propósito e ToString() não revela o conteúdo.
/// </summary>
public sealed class TaintMark
{
    internal TaintMark(PiiCategory category, string value) { Category = category; Value = value; }
    public PiiCategory Category { get; }
    internal string Value { get; }
    public override string ToString() => $"TaintMark({Category})";
}

/// <summary>Resultado estruturado da análise. O texto original não é retido; só o sanitizado e as marcas em memória.</summary>
public sealed class PiiScan
{
    internal PiiScan(string sanitized, IReadOnlyList<PiiFinding> findings, IReadOnlyList<TaintMark> marks)
    { Sanitized = sanitized; Findings = findings; Marks = marks; }
    public string Sanitized { get; }
    public IReadOnlyList<PiiFinding> Findings { get; }
    public bool HasPii => Findings.Count > 0;
    /// <summary>Ação aplicada pelo filtro: texto com trechos substituídos, ou nenhuma.</summary>
    public string Action => HasPii ? "SUBSTITUIDO" : "NENHUMA";
    internal IReadOnlyList<TaintMark> Marks { get; }
    public override string ToString() => $"PiiScan(Findings={Findings.Count})";
}

/// <summary>
/// Detecção de PII por regex e contexto. NÃO detecta qualquer dado pessoal possível
/// (ver docs/features/seguranca.md: falsos positivos e negativos documentados).
/// </summary>
public static partial class PrivacyFilter
{
    // Senhas curtas (ex.: "teste") coincidem com palavras comuns; acompanhá-las em texto livre bloquearia respostas legítimas.
    private const int MinPasswordMark = 6;
    public const string Mask = "[DADO REMOVIDO]";
    private const RegexOptions Opt = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    [GeneratedRegex(@"[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}", Opt)] private static partial Regex Email();
    [GeneratedRegex(@"(?<![\d.\-])\d{3}\.?\d{3}\.?\d{3}-?\d{2}(?!\d)", Opt)] private static partial Regex Cpf();
    [GeneratedRegex(@"\b(?:cpf)\b\W{0,12}$", Opt)] private static partial Regex CpfContext();
    // RG só com contexto ("RG", "identidade", "registro geral") para não marcar qualquer número.
    [GeneratedRegex(@"\b(?:rg|r\.g\.|identidade|registro\s+geral)\b(?:\s*(?:é|eh|n[ºo°]\.?|n[úu]mero))*\s*[:=\-]?\s*(?<v>\d{1,2}\.?\d{3}\.?\d{3}-?[\dxX]?)(?!\d)", Opt)] private static partial Regex Rg();
    // Senha: campo identificado (senha=..., password: ...) ou frase na primeira pessoa; na frase o valor precisa ter dígito ou símbolo,
    // para não tratar "minha senha é importante" como senha.
    [GeneratedRegex(@"\b(?:senha|password|passwd|pwd)\b\s*[:=]\s*(?<v>(?!\[DADO REMOVIDO\])\S+)", Opt)] private static partial Regex SenhaCampo();
    [GeneratedRegex(@"\b(?:minha|nova|atual|antiga)\s+senha\s+(?:é|eh|:)\s*(?<v>(?!\[DADO REMOVIDO\])(?=[^\s.,;!?]*[\d\p{S}\p{P}-[.,;!?]])[^\s.,;!?]+)", Opt)] private static partial Regex SenhaFrase();
    // Saúde: só afirmação pessoal contextual ("tenho diabetes", "fui diagnosticada com ...", CID).
    [GeneratedRegex(@"\b(?:tenho|estou\s+com|sou\s+portador(?:a)?\s+de|fui\s+diagnosticad[oa]\s+com|faço\s+tratamento\s+(?:de|para)|meu\s+(?:diagnóstico|laudo)\s+(?:é|foi|de))\s+(?:\w+\s+){0,2}?(?:diabetes|hiv|aids|câncer|cancer|depressão|depressao|epilepsia|hepatite|tuberculose|esquizofrenia|transtorno\s+\w+|covid(?:-19)?)\b", Opt)] private static partial Regex SaudeFrase();
    [GeneratedRegex(@"\bCID[\s-]?[A-Z]\d{2}(?:\.\d)?\b", Opt)] private static partial Regex Cid();

    private readonly record struct Span(int Start, int Length, PiiCategory Category, string Value);

    /// <summary>Analisa um campo. <paramref name="known"/> propaga marcas já detectadas em outros campos da mesma requisição.</summary>
    public static PiiScan Analyze(string? text, string path, IEnumerable<TaintMark>? known = null)
    {
        text ??= "";
        var spans = new List<Span>();
        foreach (Match m in Cpf().Matches(text))
        {
            var digits = Digits(m.Value);
            var formatted = m.Value.Length != digits.Length;
            if (formatted || IsValidCpf(digits) || CpfContext().IsMatch(text[..m.Index]))
                spans.Add(new(m.Index, m.Length, PiiCategory.Cpf, digits));
        }
        AddGroup(spans, Rg().Matches(text), "v", PiiCategory.Rg, Digits);
        AddGroup(spans, SenhaCampo().Matches(text), "v", PiiCategory.Senha, v => v);
        AddGroup(spans, SenhaFrase().Matches(text), "v", PiiCategory.Senha, v => v);
        foreach (var m in new[] { Email(), SaudeFrase(), Cid() }.SelectMany(r => r.Matches(text)).Cast<Match>())
        {
            var category = m.Value.Contains('@') ? PiiCategory.Email : PiiCategory.Saude;
            spans.Add(new(m.Index, m.Length, category, m.Value.Trim().ToLowerInvariant()));
        }
        // Propagação de taint: valores já marcados em campos anteriores são removidos também onde aparecerem soltos.
        foreach (var mark in (known ?? []).Where(k => k.Value.Length >= 4))
            for (var i = text.IndexOf(mark.Value, StringComparison.OrdinalIgnoreCase); i >= 0;
                 i = text.IndexOf(mark.Value, i + mark.Value.Length, StringComparison.OrdinalIgnoreCase))
                spans.Add(new(i, mark.Value.Length, mark.Category, mark.Value));
        // Em sobreposição vence o span que começa primeiro e, depois, o mais longo.
        var chosen = new List<Span>(); var end = 0;
        foreach (var s in spans.OrderBy(s => s.Start).ThenByDescending(s => s.Length))
            if (s.Start >= end) { chosen.Add(s); end = s.Start + s.Length; }
        var sb = new StringBuilder(); var pos = 0;
        foreach (var s in chosen) { sb.Append(text, pos, s.Start - pos).Append(Mask); pos = s.Start + s.Length; }
        sb.Append(text, pos, text.Length - pos);
        return new(sb.ToString(),
            chosen.Select(s => new PiiFinding(s.Category, path)).Distinct().ToArray(),
            chosen.Where(s => s.Value.Length >= (s.Category == PiiCategory.Senha ? MinPasswordMark : 4)).Select(s => new TaintMark(s.Category, s.Value)).ToArray());
    }

    private static void AddGroup(List<Span> spans, MatchCollection matches, string group, PiiCategory category, Func<string, string> normalize)
    {
        foreach (Match m in matches) { var g = m.Groups[group]; spans.Add(new(g.Index, g.Length, category, normalize(g.Value))); }
    }

    private static string Digits(string s) => new(s.Where(char.IsAsciiDigit).ToArray());

    private static bool IsValidCpf(string d)
    {
        if (d.Length != 11 || d.Distinct().Count() == 1) return false;
        for (var t = 9; t < 11; t++)
        {
            var sum = 0; for (var i = 0; i < t; i++) sum += (d[i] - '0') * (t + 1 - i);
            if ((sum * 10 % 11 % 10) != d[t] - '0') return false;
        }
        return true;
    }

    // Mantidos para compatibilidade com os chamadores existentes (somente texto sanitizado).
    public static string Clean(string text) => Analyze(text, "texto").Sanitized;
    public static TrocaResumida[] Clean(TrocaResumida[]? history) => (history ?? [])
        .Select(x => new TrocaResumida(Clean(x.Pergunta), Clean(x.Resposta))).ToArray();
}
