using FacilMunicipio.Api.Data;
using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Domain;
using FacilMunicipio.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace Api.Tests;
public class ApiFactory : WebApplicationFactory<Program>
{
    private static readonly SemaphoreSlim PreparationLock = new(1, 1);
    public readonly TestLlm Llm = new();
    protected override void ConfigureWebHost(IWebHostBuilder b)
    {
        b.UseEnvironment("Testing");
        b.ConfigureServices(s => { s.RemoveAll<ILlmWiki>(); s.AddSingleton<ILlmWiki>(Llm); });
    }
    public async Task PrepareAsync()
    {
        await PreparationLock.WaitAsync();
        try
        {
            using var scope = Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
            Assert.False(db.Database.HasPendingModelChanges());
            if (!await db.Usuarios.AnyAsync(x => x.Email == "operator@example.test"))
            {
                var user = new Usuario { Email = "operator@example.test", Perfil = "OPERADOR" };
                user.SenhaHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>().HashPassword(user, "OnlyForIntegration42!");
                db.Usuarios.Add(user); await db.SaveChangesAsync();
            }
        }
        finally
        {
            PreparationLock.Release();
        }
    }
}
// Test double ONLY: demonstrates boundary/contract; does not claim real LLM grounding quality.
public class TestLlm : ILlmWiki
{
    public bool Unavailable; public bool ForeignId;
    public bool Insufficient;
    public TrocaResumida[] History = []; public string Question = "";
    public ServicoRevisado? ExtractionProposal { get; set; }
    public string? ExtractionInput { get; private set; }
    public Task<ServicoRevisado> ExtrairAsync(string text, CancellationToken ct)
    {
        if (Unavailable) return Task.FromException<ServicoRevisado>(new LlmUnavailableException());
        ExtractionInput = text;
        return ExtractionProposal is null
            ? Task.FromException<ServicoRevisado>(new LlmUnavailableException())
            : Task.FromResult(ExtractionProposal);
    }
    public Task<ResultadoLlm> ResponderAsync(string pergunta, TrocaResumida[] historico, IReadOnlyList<Conhecimento> contexto, CancellationToken ct)
    {
        if (Unavailable) throw new LlmUnavailableException();
        History = historico; Question = pergunta;
        if (Insufficient) return Task.FromResult(new ResultadoLlm("", [], false));
        return Task.FromResult(new ResultadoLlm(contexto[0].Servico.Descricao,
            [ForeignId ? "foreign-service" : contexto[0].Servico.Id], true));
    }
}
