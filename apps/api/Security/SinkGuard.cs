using System.Text.RegularExpressions;
using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Data;
using FacilMunicipio.Api.Domain;
using FacilMunicipio.Api.Services;
namespace FacilMunicipio.Api.Security;

/// <summary>
/// Grava eventos de auditoria em escopo/conexão próprios (não participa de transações da requisição).
/// Falha de auditoria NUNCA libera dado: a decisão de sanitizar/bloquear já foi tomada antes; o evento
/// perdido é registrado em log apenas com o tipo da exceção. Não há fila: uma tentativa, timeout curto.
/// </summary>
public sealed partial class SecurityAudit(IServiceScopeFactory scopes, ILogger<SecurityAudit> logger)
{
    [GeneratedRegex(@"^[a-z]+(\.[a-z]+(\[\d{1,3}\])?)*$")] private static partial Regex ControlledPath();

    public async Task<bool> RecordAsync(Guid correlation, string municipioId, IEnumerable<PiiFinding> findings,
        SinkDestino destino, AcaoProtecao acao, string codigo)
    {
        var events = findings.Distinct().Select(f => new EventoSeguranca
        {
            CorrelacaoId = correlation, MunicipioId = municipioId, Categoria = f.Category, Destino = destino, Acao = acao,
            Codigo = codigo, Gravidade = RiskRules.Gravidade(f.Category),
            Origem = ControlledPath().IsMatch(f.Path) && f.Path.Length <= 80 ? f.Path : "desconhecida"
        }).ToArray();
        if (events.Length == 0) return true;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.EventosSeguranca.AddRange(events);
            await db.SaveChangesAsync(timeout.Token);
            return true;
        }
        catch (Exception e)
        {
            logger.LogWarning("Falha ao gravar auditoria de privacidade. Tipo={Tipo} Eventos={Eventos}", e.GetType().Name, events.Length);
            return false;
        }
    }
}

/// <summary>
/// Camada reutilizável de proteção das saídas (sinks) cobertas: chamada à LLM, resposta pública,
/// persistência, publicação no grafo e logs escritos por <see cref="Log"/>.
/// Fluxo: source (Analyze) → marcação (TaintMark em memória) → tratamento (sanitizar ou rejeitar)
/// → verificação do sink (Verify/RequireClean) → decisão registrada apenas com metadados.
/// Escopo: uma instância por requisição; não protege automaticamente ILogger nem o restante da aplicação.
/// </summary>
public sealed class SinkGuard(SecurityAudit audit, ILogger<SinkGuard> logger)
{
    private readonly List<TaintMark> marks = [];
    public Guid CorrelacaoId { get; } = Guid.NewGuid();
    public const string RejectionMessage = "O conteúdo informado parece conter dados pessoais (CPF, RG, e-mail, senha ou informação de saúde). Remova esses dados e envie novamente.";

    /// <summary>Política "Conversa": remove PII, propaga marcas e continua com o texto seguro.</summary>
    public async Task<string> SanitizeAsync(string municipioId, string? text, string path, SinkDestino destino)
    {
        var scan = PrivacyFilter.Analyze(text, path, marks);
        marks.AddRange(scan.Marks);
        if (scan.HasPii)
        {
            await audit.RecordAsync(CorrelacaoId, municipioId, scan.Findings, destino, AcaoProtecao.Sanitizado, "PII_SANITIZADA");
            Log(LogLevel.Information, $"PII sanitizada. Destino={destino} Categorias={Names(scan.Findings)}");
        }
        return scan.Sanitized;
    }

    /// <summary>Procura PII nova ou valor marcado (mesmo transformado) nos campos; só metadados saem.</summary>
    public IReadOnlyList<PiiFinding> Inspect(params (string Path, string? Text)[] fields)
    {
        var found = new List<PiiFinding>();
        foreach (var (path, text) in fields)
        {
            if (string.IsNullOrEmpty(text)) continue;
            found.AddRange(PrivacyFilter.Analyze(text, path).Findings);
            var digits = new string(text.Where(char.IsAsciiDigit).ToArray());
            foreach (var mark in marks)
            {
                var hit = text.Contains(mark.Value, StringComparison.OrdinalIgnoreCase)
                    || (mark.Category is PiiCategory.Cpf or PiiCategory.Rg && mark.Value.Length >= 7 && digits.Contains(mark.Value, StringComparison.Ordinal));
                if (hit) found.Add(new PiiFinding(mark.Category, path));
            }
        }
        return found.Distinct().ToArray();
    }

    /// <summary>Verificação do sink. false = saída bloqueada (decisão auditada, nenhum valor exposto).</summary>
    public async Task<bool> VerifyAsync(string municipioId, SinkDestino destino, params (string Path, string? Text)[] fields)
    {
        var findings = Inspect(fields);
        if (findings.Count == 0) return true;
        await audit.RecordAsync(CorrelacaoId, municipioId, findings, destino, AcaoProtecao.Bloqueado, "SINK_BLOQUEADO");
        Log(LogLevel.Warning, $"Saída bloqueada. Destino={destino} Categorias={Names(findings)}");
        return false;
    }

    /// <summary>Política "Fontes e rascunhos": rejeita antes da persistência/publicação com mensagem segura.</summary>
    public async Task RequireCleanAsync(string municipioId, SinkDestino destino, IEnumerable<(string Path, string? Text)> fields)
    {
        var list = fields.ToArray();
        foreach (var field in list) marks.AddRange(PrivacyFilter.Analyze(field.Text, field.Path).Marks);
        var findings = Inspect(list);
        if (findings.Count == 0) return;
        await audit.RecordAsync(CorrelacaoId, municipioId, findings, destino, AcaoProtecao.Bloqueado, "CONTEUDO_REJEITADO");
        Log(LogLevel.Warning, $"Conteúdo rejeitado. Destino={destino} Categorias={Names(findings)}");
        throw new ApiException(422, RejectionMessage);
    }

    /// <summary>
    /// Saída de log protegida: se a mensagem contiver PII ou valor marcado, o texto não é escrito;
    /// só o aviso do bloqueio com categorias. Usada em todos os logs novos deste módulo.
    /// </summary>
    public void Log(LogLevel level, string message)
    {
        var findings = Inspect(("log.mensagem", message));
        if (findings.Count == 0) { logger.Log(level, "{Mensagem}", message); return; }
        logger.LogWarning("Log bloqueado: mensagem continha possível dado pessoal. Categorias={Categorias}", Names(findings));
    }

    private static string Names(IEnumerable<PiiFinding> f) => string.Join(",", f.Select(x => x.Category).Distinct());

    public static IEnumerable<(string Path, string? Text)> Fields(ServicoRevisado s)
    {
        yield return ("rascunho.id", s.Id); yield return ("rascunho.nome", s.Nome); yield return ("rascunho.descricao", s.Descricao);
        yield return ("rascunho.orgao", s.Orgao); yield return ("rascunho.canal", s.CanalUrl);
        for (var i = 0; i < (s.Etapas?.Length ?? 0); i++) yield return ($"rascunho.etapas[{i}]", s.Etapas![i]);
        for (var i = 0; i < (s.Requisitos?.Length ?? 0); i++) yield return ($"rascunho.requisitos[{i}]", s.Requisitos![i]);
        for (var i = 0; i < (s.Termos?.Length ?? 0); i++) yield return ($"rascunho.termos[{i}]", s.Termos![i]);
    }
}
