using FacilMunicipio.Api.Contracts;
namespace FacilMunicipio.Api.Services;
public interface IKnowledgeGraph
{
    Task WriteAsync(string municipioId, Guid versao, IReadOnlyList<Conhecimento> servicos, CancellationToken ct);
    Task<IReadOnlyList<Conhecimento>> ReadAsync(string municipioId, Guid versao, CancellationToken ct);
}
// Integration seam owned jointly with P3: no provider, key, free Cypher or publication in the LLM.
public interface ILlmWiki
{
    Task<ServicoRevisado> ExtrairAsync(string texto, CancellationToken ct);
    Task<ResultadoLlm> ResponderAsync(string pergunta, TrocaResumida[] historico,
        IReadOnlyList<Conhecimento> contexto, CancellationToken ct);
}
public sealed class LlmUnavailable : ILlmWiki
{
    public Task<ServicoRevisado> ExtrairAsync(string texto, CancellationToken ct) => throw new LlmUnavailableException();
    public Task<ResultadoLlm> ResponderAsync(string pergunta, TrocaResumida[] historico,
        IReadOnlyList<Conhecimento> contexto, CancellationToken ct) => throw new LlmUnavailableException();
}
public sealed class LlmUnavailableException : Exception { }
public sealed class ApiException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}
