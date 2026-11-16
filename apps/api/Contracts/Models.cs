using System.ComponentModel.DataAnnotations;

namespace FacilMunicipio.Api.Contracts;

public record MunicipioDto(string Id, string Nome, string Uf);
public record ServicoDto(string Id, string Nome);
public record FonteDto(string Titulo, string Url, string? DataConsulta);
public record RespostaConversa(string Resposta, string[] ServicosRelacionados, FonteDto[] Fontes,
    bool InformacaoSuficiente, string? CanalOficialUrl = null)
{
    public static RespostaConversa Insuficiente(string? canal) => new(
        "Não encontrei informação suficiente na base publicada deste município. Consulte o canal oficial para confirmar a orientação.",
        [], [], false, canal);
    public static RespostaConversa Bloqueada(string? canal) => new(
        "Não foi possível exibir esta resposta com segurança. Consulte o canal oficial para confirmar a orientação.",
        [], [], false, canal);
}
public record TrocaResumida(
    [param: Required, StringLength(1000, MinimumLength = 1)] string Pergunta,
    [param: Required, StringLength(1000, MinimumLength = 1)] string Resposta);
public record EnvioPergunta(
    [param: Required, StringLength(500, MinimumLength = 1)] string Pergunta,
    [param: MaxLength(3)] TrocaResumida[]? Historico);
public record LoginRequest(
    [param: Required, EmailAddress, StringLength(254)] string Email,
    [param: Required, StringLength(200, MinimumLength = 1)] string Senha);
public record CriarMunicipio(
    [param: Required, RegularExpression("^[a-z0-9-]{1,60}$")] string Id,
    [param: Required, StringLength(150, MinimumLength = 1)] string Nome,
    [param: Required, RegularExpression("^[A-Z]{2}$")] string Uf,
    [param: HttpsUrl] string? CanalOficialUrl);
public record CriarUsuario(
    [param: Required, EmailAddress, StringLength(254)] string Email,
    [param: Required, StringLength(200, MinimumLength = 12)] string Senha,
    [param: Required] string MunicipioId);
public record CriarFonte(
    [param: Required, StringLength(200, MinimumLength = 1)] string Titulo,
    [param: Required, HttpsUrl] string Url,
    [param: Required, StringLength(30000, MinimumLength = 1)] string Texto,
    DateOnly DataConsulta);
// One reviewed source supports this service. The graph stores each component linked to it.
public record ServicoRevisado(
    [param: Required, RegularExpression("^[a-z0-9-]{1,60}$")] string Id,
    [param: Required, StringLength(150, MinimumLength = 1)] string Nome,
    [param: Required, StringLength(4000, MinimumLength = 1)] string Descricao,
    [param: StringLength(200)] string? Orgao,
    [param: Required, MaxLength(20)] string[] Etapas,
    [param: Required, MaxLength(20)] string[] Requisitos,
    [param: HttpsUrl] string? CanalUrl,
    [param: Required, MaxLength(30)] string[] Termos) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if ((Etapas ?? []).Concat(Requisitos ?? []).Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 1000))
            yield return new("Etapas e requisitos devem ter de 1 a 1000 caracteres.");
        if ((Termos ?? []).Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 100))
            yield return new("Termos devem ter de 1 a 100 caracteres.");
    }
}
public record RevisarRascunho([param: Required] ServicoRevisado Servico, bool Aprovado, Guid RevisaoEsperada);
public record CriarRascunho([param: Required] ServicoRevisado Servico);
public record Conhecimento(ServicoRevisado Servico, FonteDto Fonte);
public record ResultadoLlm(string Resposta, string[] ServicoIds, bool InformacaoSuficiente);
public sealed class HttpsUrlAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is null || value is string s &&
        Uri.TryCreate(s, UriKind.Absolute, out var u) && u.Scheme == "https" && string.IsNullOrEmpty(u.UserInfo) && s.Length <= 2048;
}
// Painel de auditoria de privacidade. Somente metadados; nunca conteúdo original.
public record IndicadorRisco(string Nivel, string Rotulo, string Descricao);
public record ResumoSeguranca(int Total, int Sanitizados, int Bloqueados, int Pendentes, int TraducoesPendentes);
public record EventoSegurancaDto(Guid Id, DateTimeOffset OcorridoEm, string Categoria, string Origem, string Destino,
    string Acao, string Gravidade, string Codigo, string TraducaoEstado, string Explicacao, string OrigemExplicacao);
public record PainelSeguranca(int PeriodoHoras, DateTimeOffset GeradoEm, IndicadorRisco Indicador, ResumoSeguranca Resumo, EventoSegurancaDto[] Eventos);
