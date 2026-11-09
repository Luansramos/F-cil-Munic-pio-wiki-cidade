using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Data;
using FacilMunicipio.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace Api.Tests;
public class IntegrationTests
{
    private static async Task<JsonElement> Json(HttpResponseMessage r)
    {
        r.EnsureSuccessStatusCode();
        return (await JsonDocument.ParseAsync(await r.Content.ReadAsStreamAsync())).RootElement.Clone();
    }
    [Fact]
    public async Task RealDatabases_PublicationIsolationAuthorizationAndMobileContract()
    {
        await using var factory = new ApiFactory(); await factory.PrepareAsync();
        using var admin = factory.CreateClient(); using var citizen = factory.CreateClient();
        var login = await Json(await admin.PostAsJsonAsync("/api/v1/auth/login", new { email = "operator@example.test", senha = "OnlyForIntegration42!" }));
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("token").GetString());
        var city = "test-" + Guid.NewGuid().ToString("N")[..8]; var other = city + "-other";
        foreach (var id in new[] { city, other })
            await Json(await admin.PostAsJsonAsync("/api/v1/municipios", new { id, nome = "Município fictício de teste", uf = "SP" }));
        Assert.Equal(HttpStatusCode.Unauthorized, (await citizen.PostAsJsonAsync("/api/v1/municipios", new { id = "x", nome = "x", uf = "SP" })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await admin.PostAsJsonAsync("/api/v1/municipios", new { id = "Cidade Invalida", nome = "Teste", uf = "SP" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PostAsJsonAsync("/api/v1/municipios", new { id = city, nome = "Duplicada", uf = "SP" })).StatusCode);
        Assert.Empty(await citizen.GetFromJsonAsync<ServicoDto[]>($"/api/v1/municipios/{city}/servicos") ?? []);
        Assert.Equal(HttpStatusCode.NotFound, (await citizen.GetAsync("/api/v1/municipios/unknown/servicos")).StatusCode);
        var insufficient = await Json(await citizen.PostAsJsonAsync($"/api/v1/municipios/{city}/conversas", new { pergunta = "Como funciona?" }));
        Assert.False(insufficient.GetProperty("informacaoSuficiente").GetBoolean());
        Assert.Empty(insufficient.GetProperty("fontes").EnumerateArray());
        Assert.False(insufficient.TryGetProperty("canalOficialUrl", out _));
        // Source is explicitly fictional, not official Poá content.
        var source = await Json(await admin.PostAsJsonAsync($"/api/v1/municipios/{city}/fontes", new {
            titulo = "Fonte fictícia para teste", url = "https://example.org/servico", texto = "Teste: consulte o canal de demonstração.", dataConsulta = "2026-01-01" }));
        var sourceId = source.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await admin.PostAsJsonAsync($"/api/v1/municipios/{city}/fontes", new {
                titulo = "Fonte insegura", url = "http://example.org/inseguro",
                texto = "Conteúdo fictício de teste", dataConsulta = "2026-01-01"
            })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await admin.PostAsJsonAsync($"/api/v1/municipios/{city}/fontes", new {
                titulo = "Consulta futura", url = "https://example.org/futuro",
                texto = "Conteúdo fictício de teste", dataConsulta = "2099-01-01"
            })).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await admin.PostAsync($"/api/v1/fontes/{sourceId}/extracoes", null)).StatusCode);
        var service = new ServicoRevisado("cata-treco", "Cata Treco", "Orientação fictícia para teste.", "Órgão fictício", ["Consulte o canal de demonstração."], [], null, ["sofá"]);
        var draft = await Json(await admin.PostAsJsonAsync($"/api/v1/fontes/{sourceId}/rascunhos", new CriarRascunho(service)));
        var draftId = draft.GetProperty("id").GetGuid(); var revision = draft.GetProperty("revisaoId").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/api/v1/rascunhos/{draftId}/publicacao", null)).StatusCode);
        await Json(await admin.PutAsJsonAsync($"/api/v1/rascunhos/{draftId}", new RevisarRascunho(service, true, revision)));
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/v1/rascunhos/{draftId}", new RevisarRascunho(service, true, revision))).StatusCode);
        var publication = await Json(await admin.PostAsync($"/api/v1/rascunhos/{draftId}/publicacao", null));
        var retry = await Json(await admin.PostAsync($"/api/v1/rascunhos/{draftId}/publicacao", null));
        Assert.Equal(publication.GetProperty("id").GetGuid(), retry.GetProperty("id").GetGuid());
        var services = await citizen.GetFromJsonAsync<ServicoDto[]>($"/api/v1/municipios/{city}/servicos");
        Assert.Single(services!); Assert.Equal("cata-treco", services![0].Id);
        Assert.Empty(await citizen.GetFromJsonAsync<ServicoDto[]>($"/api/v1/municipios/{other}/servicos") ?? []);
        var answer = await Json(await citizen.PostAsJsonAsync($"/api/v1/municipios/{city}/conversas", new EnvioPergunta("E no domingo? CPF 123.456.789-00", [new("Retirada de sofá", "senha=teste")])));
        Assert.True(answer.GetProperty("informacaoSuficiente").GetBoolean());
        Assert.Equal("cata-treco", answer.GetProperty("servicosRelacionados")[0].GetString());
        Assert.Equal("https://example.org/servico", answer.GetProperty("fontes")[0].GetProperty("url").GetString());
        Assert.DoesNotContain("123.456", factory.Llm.Question); Assert.DoesNotContain("senha=teste", factory.Llm.History[0].Resposta);
        factory.Llm.ForeignId = true;
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await citizen.PostAsJsonAsync($"/api/v1/municipios/{city}/conversas", new { pergunta = "Teste" })).StatusCode);
        factory.Llm.ForeignId = false; factory.Llm.Unavailable = true;
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await citizen.PostAsJsonAsync($"/api/v1/municipios/{city}/conversas", new { pergunta = "Teste" })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await citizen.PostAsJsonAsync($"/api/v1/municipios/{city}/conversas", new { pergunta = new string('x',501) })).StatusCode);
        // Municipal administrator cannot access another city or create cities.
        var email = city + "@example.test";
        await Json(await admin.PostAsJsonAsync("/api/v1/usuarios", new { email, senha = "OnlyForIntegration42!", municipioId = other }));
        using var municipal = factory.CreateClient();
        var municipalLogin = await Json(await municipal.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = "OnlyForIntegration42!" }));
        municipal.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", municipalLogin.GetProperty("token").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await municipal.GetAsync($"/api/v1/rascunhos/{draftId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await municipal.GetAsync($"/api/v1/municipios/{city}/fontes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await municipal.PostAsync($"/api/v1/rascunhos/{draftId}/publicacao", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await municipal.PostAsJsonAsync("/api/v1/municipios", new { id = "denied", nome = "Denied", uf = "SP" })).StatusCode);
        // Revise, inject graph outage, and prove old active version survives the rolled-back transaction.
        draft = await Json(await admin.GetAsync($"/api/v1/rascunhos/{draftId}"));
        var revised = await Json(await admin.PutAsJsonAsync($"/api/v1/rascunhos/{draftId}", new RevisarRascunho(service with { Descricao = "Nova revisão fictícia" }, true, draft.GetProperty("revisaoId").GetGuid())));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var accessor = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            accessor.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([
                new(System.Security.Claims.ClaimTypes.Role,"OPERADOR")], "test")) };
            var publisher = new PublicationService(db, new FailingGraph(scope.ServiceProvider.GetRequiredService<IKnowledgeGraph>()), new(accessor));
            await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAsync(draftId, default));
        }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(publication.GetProperty("id").GetGuid(), (await db.Municipios.SingleAsync(x => x.Id == city)).VersaoAtivaId);
        }
        await Json(await admin.PostAsync($"/api/v1/rascunhos/{draftId}/publicacao", null));
        Assert.Single(await citizen.GetFromJsonAsync<ServicoDto[]>($"/api/v1/municipios/{city}/servicos") ?? []);
        // OpenAPI generation and rate-limit behavior are verified through actual HTTP middleware.
        Assert.Equal(HttpStatusCode.OK, (await citizen.GetAsync("/swagger/v1/swagger.json")).StatusCode);
        HttpResponseMessage? last = null;
        for (var i = 0; i < 12; i++) last = await citizen.PostAsJsonAsync($"/api/v1/municipios/{other}/conversas", new { pergunta = "Teste" });
        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }
    private class FailingGraph(IKnowledgeGraph graph) : IKnowledgeGraph
    {
        public Task<IReadOnlyList<Conhecimento>> ReadAsync(string m, Guid v, CancellationToken ct) => graph.ReadAsync(m,v,ct);
        public async Task WriteAsync(string m, Guid v, IReadOnlyList<Conhecimento> k, CancellationToken ct)
        {
            await graph.WriteAsync(m,v,k,ct); throw new InvalidOperationException("Falha simulada após gravar grafo");
        }
    }
}
