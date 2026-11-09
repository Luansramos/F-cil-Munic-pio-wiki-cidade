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
}
public record TrocaResumida(
    [property: Required, StringLength(1000, MinimumLength = 1)] string Pergunta,
    [property: Required, StringLength(1000, MinimumLength = 1)] string Resposta);
public record EnvioPergunta(
    [property: Required, StringLength(500, MinimumLength = 1)] string Pergunta,
    [property: MaxLength(3)] TrocaResumida[]? Historico);
public record LoginRequest(
    [property: Required, EmailAddress, StringLength(254)] string Email,
    [property: Required, StringLength(200, MinimumLength = 1)] string Senha);
public record CriarMunicipio(
    [property: Required, RegularExpression("^[a-z0-9-]{1,60}$")] string Id,
    [property: Required, StringLength(150, MinimumLength = 1)] string Nome,
    [property: Required, RegularExpression("^[A-Z]{2}$")] string Uf,
    [property: HttpsUrl] string? CanalOficialUrl);
public record CriarUsuario(
    [property: Required, EmailAddress, StringLength(254)] string Email,
    [property: Required, StringLength(200, MinimumLength = 12)] string Senha,
    [property: Required] string MunicipioId);
public record CriarFonte(
    [property: Required, StringLength(200, MinimumLength = 1)] string Titulo,
    [property: Required, HttpsUrl] string Url,
    [property: Required, StringLength(30000, MinimumLength = 1)] string Texto,
    DateOnly DataConsulta);
// One reviewed source supports this service. The graph stores each component linked to it.
public record ServicoRevisado(
    [property: Required, RegularExpression("^[a-z0-9-]{1,60}$")] string Id,
    [property: Required, StringLength(150, MinimumLength = 1)] string Nome,
    [property: Required, StringLength(4000, MinimumLength = 1)] string Descricao,
    [property: StringLength(200)] string? Orgao,
    [property: Required, MaxLength(20)] string[] Etapas,
    [property: Required, MaxLength(20)] string[] Requisitos,
    [property: HttpsUrl] string? CanalUrl,
    [property: Required, MaxLength(30)] string[] Termos) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if ((Etapas ?? []).Concat(Requisitos ?? []).Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 1000))
            yield return new("Etapas e requisitos devem ter de 1 a 1000 caracteres.");
        if ((Termos ?? []).Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 100))
            yield return new("Termos devem ter de 1 a 100 caracteres.");
    }
}
public record RevisarRascunho([property: Required] ServicoRevisado Servico, bool Aprovado, Guid RevisaoEsperada);
public record CriarRascunho([property: Required] ServicoRevisado Servico);
public record Conhecimento(ServicoRevisado Servico, FonteDto Fonte);
public record ResultadoLlm(string Resposta, string[] ServicoIds, bool InformacaoSuficiente);
public sealed class HttpsUrlAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is null || value is string s &&
        Uri.TryCreate(s, UriKind.Absolute, out var u) && u.Scheme == "https" && string.IsNullOrEmpty(u.UserInfo) && s.Length <= 2048;
}
