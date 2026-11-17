namespace FacilMunicipio.Api.Domain;

// Categorias de dado pessoal detectadas. Gravidade potencial vem de regra em RiskRules, não da IA.
public enum PiiCategory { Cpf, Rg, Email, Senha, Saude }
// Destino (sink) onde o dado marcado poderia ter sido usado.
public enum SinkDestino { Llm, RespostaPublica, Persistencia, Grafo }
// Situação da proteção. Pendente = decisão de proteção ainda não concluída (reservada; nenhum caminho atual a emite).
public enum AcaoProtecao { Sanitizado, Bloqueado, Pendente }
public enum Gravidade { Baixa, Media, Alta }
public enum TraducaoEstado { Pendente, Traduzida, Falhou }

/// <summary>
/// Evento de auditoria de privacidade. Contém SOMENTE metadados controlados: nunca o texto original,
/// o valor detectado, hash de identificador, senha, token, corpo HTTP, IP ou dispositivo.
/// </summary>
public class EventoSeguranca
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Identificador aleatório da operação; não é derivado de pessoa, IP ou dispositivo.</summary>
    public Guid CorrelacaoId { get; set; }
    public string MunicipioId { get; set; } = "";
    public DateTimeOffset OcorridoEm { get; set; } = DateTimeOffset.UtcNow;
    public PiiCategory Categoria { get; set; }
    /// <summary>Caminho controlado do campo (ex.: conversa.historico[0].resposta).</summary>
    public string Origem { get; set; } = "";
    public SinkDestino Destino { get; set; }
    public AcaoProtecao Acao { get; set; }
    public Gravidade Gravidade { get; set; }
    /// <summary>Código técnico fixo (ex.: PII_SANITIZADA, SINK_BLOQUEADO).</summary>
    public string Codigo { get; set; } = "";
    public TraducaoEstado TraducaoEstado { get; set; } = TraducaoEstado.Pendente;
    public int TraducaoTentativas { get; set; }
    public string? TraducaoTexto { get; set; }
    /// <summary>Identificador do modelo de IA que gerou a explicação (vem da configuração; nulo sem tradução).</summary>
    public string? TraducaoModelo { get; set; }
    public DateTimeOffset? TraduzidaEm { get; set; }
}
