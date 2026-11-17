using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Data;
using FacilMunicipio.Api.Domain;
using FacilMunicipio.Api.Security;
using Microsoft.EntityFrameworkCore;
namespace FacilMunicipio.Api.Services;

/// <summary>Regras do código (não da IA): gravidade potencial, indicador Risco Cidadão e textos controlados.</summary>
public static class RiskRules
{
    /// <summary>Gravidade POTENCIAL do tipo de dado, independente de a proteção ter funcionado.</summary>
    public static Gravidade Gravidade(PiiCategory c) => c switch
    {
        PiiCategory.Senha or PiiCategory.Saude => Domain.Gravidade.Alta,
        PiiCategory.Cpf or PiiCategory.Rg => Domain.Gravidade.Media,
        _ => Domain.Gravidade.Baixa
    };

    /// <summary>
    /// Indicador do período (separa gravidade potencial de situação da proteção):
    /// VERMELHO = há evento com proteção PENDENTE e gravidade ALTA (proteção não confirmada);
    /// AMARELO = há evento PENDENTE, ou evento tratado (sanitizado/bloqueado) de gravidade MEDIA/ALTA;
    /// VERDE = nenhum evento, ou somente eventos tratados de gravidade BAIXA.
    /// Bloqueio bem-sucedido nunca conta como vazamento confirmado.
    /// </summary>
    public static IndicadorRisco Indicador(IReadOnlyCollection<(AcaoProtecao Acao, Gravidade Gravidade)> events, int horas)
    {
        if (events.Any(e => e.Acao == AcaoProtecao.Pendente && e.Gravidade == Domain.Gravidade.Alta))
            return new("VERMELHO", "Atenção: proteção pendente", $"Há eventos de gravidade alta cuja proteção ainda não foi confirmada nas últimas {horas} h. Requer revisão.");
        if (events.Any(e => e.Acao == AcaoProtecao.Pendente || e.Gravidade != Domain.Gravidade.Baixa))
            return new("AMARELO", "Monitorar", $"Dados pessoais foram detectados e tratados (removidos ou bloqueados) nas últimas {horas} h. Tratamento bem-sucedido não significa que dados foram expostos.");
        return events.Count == 0
            ? new("VERDE", "Sem eventos registrados", $"Nenhum evento nas últimas {horas} h. Isso não é garantia de segurança: só aparecem casos detectados pelas regras instrumentadas.")
            : new("VERDE", "Sem alertas relevantes", $"Somente eventos de gravidade baixa, já tratados, nas últimas {horas} h.");
    }

    /// <summary>Mensagem padrão do sistema, usada quando não há tradução por IA. Nunca é apresentada como resposta de IA.</summary>
    public static string MensagemPadrao(AcaoProtecao acao) => acao switch
    {
        AcaoProtecao.Sanitizado => "Uma informação pessoal foi removida do texto antes de ele ser processado.",
        AcaoProtecao.Bloqueado => "Uma saída foi bloqueada porque poderia conter informação pessoal.",
        _ => "Um evento de privacidade aguarda uma decisão de proteção."
    };

    /// <summary>O que o código do evento significa, em texto controlado, para a IA não inventar motivos.</summary>
    public static string DescricaoCodigo(string codigo) => codigo switch
    {
        "PII_SANITIZADA" => "o dado pessoal foi removido do texto antes de ele seguir adiante e a conversa continuou sem esse dado",
        "SINK_BLOQUEADO" => "a saída foi impedida porque continha dado pessoal; nada foi exibido nem enviado",
        "CONTEUDO_REJEITADO" => "o cadastro foi recusado porque o texto continha dado pessoal; nada foi gravado no banco de dados nem publicado",
        _ => "o sistema registrou um evento de proteção de privacidade"
    };

    public static string RotuloCategoria(PiiCategory c) => c switch
    {
        PiiCategory.Cpf => "CPF", PiiCategory.Rg => "RG", PiiCategory.Email => "e-mail",
        PiiCategory.Senha => "senha", _ => "informação de saúde"
    };
    public static string RotuloOrigem(string origem) =>
        origem.StartsWith("conversa.") ? "pergunta ou histórico da conversa" :
        origem.StartsWith("llm.") ? "resposta gerada pelo assistente" :
        origem.StartsWith("fonte.") ? "texto de uma fonte administrativa" :
        origem.StartsWith("grafo.") ? "conhecimento já publicado" :
        origem.StartsWith("resposta.") ? "resposta pública" :
        origem.StartsWith("rascunho.") ? "rascunho de serviço" : "campo do sistema";
    public static string RotuloDestino(SinkDestino d) => d switch
    {
        SinkDestino.Llm => "envio ao assistente de IA", SinkDestino.RespostaPublica => "resposta pública ao cidadão",
        SinkDestino.Persistencia => "gravação no banco de dados", _ => "publicação no grafo de conhecimento"
    };
}

public interface IRiskTranslator
{
    /// <summary>false quando não há credencial/configuração: o evento fica PENDENTE sem consumir tentativas.</summary>
    bool Configured { get; }
    /// <summary>Modelo configurado, registrado no evento para evidência (valor da configuração, nunca a chave).</summary>
    string ModeloUsado { get; }
    Task<string> TranslateAsync(EventoSeguranca evento, CancellationToken ct);
}

/// <summary>
/// Provedor real por API compatível com "chat completions" (OpenAI, Gemini em modo compatível etc.).
/// Endpoint, modelo e chave vêm do ambiente; nenhum modelo é fixado no código (confirme na documentação oficial do provedor).
/// O prompt contém somente metadados controlados do evento; nunca texto original nem mensagem de exceção.
/// </summary>
public sealed class OpenAiCompatibleRiskTranslator(HttpClient http, IConfiguration config) : IRiskTranslator
{
    private string BaseUrl => (config["RiskTranslator:BaseUrl"] ?? "").TrimEnd('/');
    private string Model => config["RiskTranslator:Model"] ?? "";
    private string ApiKey => config["RiskTranslator:ApiKey"] ?? "";
    public bool Configured => BaseUrl.StartsWith("https://") && Model.Length > 0 && ApiKey.Length > 0;
    public string ModeloUsado => Model;

    public static string BuildPrompt(EventoSeguranca e) =>
        "Explique para um cidadão leigo de 60 anos um evento do sistema de privacidade do Fácil Município. " +
        "Dados do evento (controlados pelo sistema): " +
        $"tipo de dado = {RiskRules.RotuloCategoria(e.Categoria)}; origem = {RiskRules.RotuloOrigem(e.Origem)}; " +
        $"destino = {RiskRules.RotuloDestino(e.Destino)}; gravidade potencial = {e.Gravidade}; " +
        $"o que o sistema fez = {RiskRules.DescricaoCodigo(e.Codigo)}. " +
        "Regras: responda em português do Brasil, em no máximo duas frases, com linguagem simples; " +
        "descreva fielmente o que o sistema fez, usando só as informações acima e sem inventar outros motivos; " +
        "não afirme ataque, acesso indevido ou vazamento, pois nada disso foi comprovado; não mencione códigos técnicos; não peça nem repita dados pessoais.";

    public async Task<string> TranslateAsync(EventoSeguranca e, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
        request.Content = JsonContent.Create(new { model = Model, messages = new[] { new { role = "user", content = BuildPrompt(e) } } });
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}

/// <summary>Processa traduções pendentes em lote limitado, com tentativas máximas e reaproveitamento de textos iguais.</summary>
public sealed partial class RiskTranslationProcessor(AppDbContext db, IRiskTranslator translator, ILogger<RiskTranslationProcessor> logger)
{
    public const int MaxAttempts = 3;
    /// <summary>true se o último lote teve falha: o worker espera antes de tentar de novo (provedor fora do ar ou configuração errada).</summary>
    public bool LastRunFailed { get; private set; }
    [GeneratedRegex(@"\b(ataque|atacante|invas[ãa]o|invadi\w*|hacker|vazou|vazamento confirmado)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)] private static partial Regex Claims();
    [GeneratedRegex(@"[.!?]+(\s|$)")] private static partial Regex SentenceEnd();

    /// <summary>A saída da IA também é verificada: tamanho, no máximo 2 frases, sem PII e sem alegação de ataque.</summary>
    public static bool IsAcceptable(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 400) return false;
        if (SentenceEnd().Matches(text.Trim()).Count > 2 || Claims().IsMatch(text)) return false;
        return !PrivacyFilter.Analyze(text, "llm.traducao").HasPii;
    }

    public async Task<int> ProcessPendingAsync(int batch, CancellationToken ct)
    {
        if (!translator.Configured) return 0;
        var pending = await db.EventosSeguranca.Where(e => e.TraducaoEstado == TraducaoEstado.Pendente && e.TraducaoTentativas < MaxAttempts)
            .OrderBy(e => e.OcorridoEm).Take(batch).ToListAsync(ct);
        foreach (var e in pending)
        {
            var failed = false;
            var reused = await db.EventosSeguranca.Where(x => x.TraducaoEstado == TraducaoEstado.Traduzida && x.Codigo == e.Codigo
                && x.Categoria == e.Categoria && x.Origem == e.Origem && x.Destino == e.Destino && x.Acao == e.Acao && x.Gravidade == e.Gravidade)
                .OrderBy(x => x.OcorridoEm).Select(x => new { x.TraducaoTexto, x.TraducaoModelo }).FirstOrDefaultAsync(ct);
            try
            {
                var text = reused?.TraducaoTexto; var model = reused?.TraducaoModelo;
                if (text is null)
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeout.CancelAfter(TimeSpan.FromSeconds(10));
                    text = (await translator.TranslateAsync(e, timeout.Token)).Trim();
                    model = translator.ModeloUsado;
                }
                if (!IsAcceptable(text)) throw new InvalidDataException();
                e.TraducaoTexto = text; e.TraducaoModelo = model is null ? null : (model.Length > 80 ? model[..80] : model); e.TraducaoEstado = TraducaoEstado.Traduzida; e.TraduzidaEm = DateTimeOffset.UtcNow;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                e.TraducaoTentativas++;
                if (e.TraducaoTentativas >= MaxAttempts) e.TraducaoEstado = TraducaoEstado.Falhou;
                // Só metadados: tipo da exceção e código HTTP (nunca corpo da resposta nem prompt).
                logger.LogWarning("Tradução de risco falhou. Tipo={Tipo} Http={Http} Tentativa={Tentativa}",
                    ex.GetType().Name, (ex as HttpRequestException)?.StatusCode is { } code ? (int)code : 0, e.TraducaoTentativas);
                failed = true;
            }
            await db.SaveChangesAsync(ct);
            // Uma falha encerra o lote: os demais eventos não gastam tentativas à toa e o provedor não é martelado.
            if (failed) { LastRunFailed = true; break; }
        }
        return pending.Count;
    }

    /// <summary>Retenção: remove eventos mais antigos que o limite, em lotes.</summary>
    public async Task<int> PurgeExpiredAsync(int retentionDays, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);
        var old = await db.EventosSeguranca.Where(e => e.OcorridoEm < cutoff).OrderBy(e => e.OcorridoEm).Take(500).ToListAsync(ct);
        db.EventosSeguranca.RemoveRange(old);
        await db.SaveChangesAsync(ct);
        return old.Count;
    }
}

/// <summary>Um único consumidor sequencial (fora do caminho crítico da conversa); sem fila na memória.</summary>
public sealed class RiskTranslationWorker(IServiceScopeFactory scopes, IConfiguration config, ILogger<RiskTranslationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(5, config.GetValue("RiskTranslator:PollSeconds", 10)));
        var retention = config.GetValue("Security:RetentionDays", 90);
        using var timer = new PeriodicTimer(interval);
        var ticks = 0; var backoffTicks = 0;
        try
        {
            while (await timer.WaitForNextTickAsync(stop))
            {
                if (backoffTicks > 0) { backoffTicks--; continue; }
                try
                {
                    using var scope = scopes.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<RiskTranslationProcessor>();
                    await processor.ProcessPendingAsync(5, stop);
                    // Após falha, espera ~60 s antes do próximo lote.
                    if (processor.LastRunFailed) backoffTicks = Math.Max(1, (int)(60 / interval.TotalSeconds));
                    if (ticks++ % 360 == 0) await processor.PurgeExpiredAsync(retention, stop);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    logger.LogWarning("Ciclo de tradução de risco falhou. Tipo={Tipo}", e.GetType().Name);
                }
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            // Encerramento normal da aplicação: não é falha do serviço.
        }
    }
}
