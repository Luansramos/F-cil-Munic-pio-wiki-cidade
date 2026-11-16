using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Data;
using FacilMunicipio.Api.Domain;
using FacilMunicipio.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
namespace Api.Tests;

/// <summary>
/// API real em memória (EF InMemory, grafo e LLM simulados): sem PostgreSQL, Neo4j nem rede.
/// Só dados simulados. Transações e SQL específicos do PostgreSQL (publicação) ficam nos testes de integração.
/// </summary>
public class SecurityFactory : WebApplicationFactory<Program>
{
    public const string JwtKey = "security-tests-only-key-0123456789-abcdef";
    public const string CityA = "cidade-a", CityB = "cidade-b";
    private readonly string dbName = Guid.NewGuid().ToString("N");
    public readonly FakeLlm Llm = new();
    public readonly LogCapture Logs = new();
    static SecurityFactory() => Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);

    protected override void ConfigureWebHost(IWebHostBuilder b)
    {
        b.UseEnvironment("Testing");
        b.UseSetting("RiskTranslator:WorkerEnabled", "false");
        b.ConfigureLogging(l => l.AddProvider(Logs));
        b.ConfigureServices(s =>
        {
            s.RemoveAll<DbContextOptions<AppDbContext>>(); s.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            s.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
            s.RemoveAll<ILlmWiki>(); s.AddSingleton<ILlmWiki>(Llm);
            s.RemoveAll<IKnowledgeGraph>(); s.AddSingleton<IKnowledgeGraph, FakeGraph>();
        });
    }

    public async Task SeedAsync(bool publishedA = true)
    {
        using var scope = Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Municipios.Add(new Municipio { Id = CityA, Nome = "Cidade A fictícia", Uf = "SP", VersaoAtivaId = publishedA ? Guid.NewGuid() : null });
        db.Municipios.Add(new Municipio { Id = CityB, Nome = "Cidade B fictícia", Uf = "SP", VersaoAtivaId = Guid.NewGuid() });
        await db.SaveChangesAsync();
    }

    public HttpClient Client(string? role = null, string? city = null)
    {
        var client = CreateClient();
        if (role is null) return client;
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), new(ClaimTypes.Role, role) };
        if (city is not null) claims.Add(new("municipioId", city));
        var jwt = new JwtSecurityToken("facil-municipio", "facil-admin", claims, expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        return client;
    }

    public async Task<T> WithDb<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}

public class FakeGraph : IKnowledgeGraph
{
    public Task WriteAsync(string m, Guid v, IReadOnlyList<Conhecimento> k, CancellationToken ct) => Task.CompletedTask;
    public Task<IReadOnlyList<Conhecimento>> ReadAsync(string m, Guid v, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Conhecimento>>([new(
            new("cata-treco", "Cata Treco", "Orientação fictícia para teste.", "Órgão fictício", ["Consulte o canal de demonstração."], [], null, ["sofá"]),
            new("Fonte fictícia", "https://example.org/servico", "2026-01-01"))]);
}

/// <summary>Dublê de teste da LLM municipal: registra o que recebeu e devolve o texto configurado.</summary>
public class FakeLlm : ILlmWiki
{
    public string? Reply; public string Question = ""; public TrocaResumida[] History = []; public int Calls;
    public Task<ServicoRevisado> ExtrairAsync(string texto, CancellationToken ct) => throw new LlmUnavailableException();
    public Task<ResultadoLlm> ResponderAsync(string pergunta, TrocaResumida[] historico, IReadOnlyList<Conhecimento> contexto, CancellationToken ct)
    {
        Calls++; Question = pergunta; History = historico;
        return Task.FromResult(new ResultadoLlm(Reply ?? contexto[0].Servico.Descricao, [contexto[0].Servico.Id], true));
    }
}

public sealed class LogCapture : ILoggerProvider
{
    private readonly List<string> lines = [];
    public string All { get { lock (lines) return string.Join("\n", lines); } }
    public ILogger CreateLogger(string category) => new Capture(this);
    public void Dispose() { }
    private sealed class Capture(LogCapture owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? e, Func<TState, Exception?, string> formatter)
        { lock (owner.lines) owner.lines.Add(formatter(state, e) + (e is null ? "" : " " + e)); }
    }
}
