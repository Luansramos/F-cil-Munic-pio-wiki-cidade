using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FacilMunicipio.Api.Contracts;

namespace Api.Tests;

public class LlmApiIntegrationTests
{
    private const string SourceUrl = "https://poa.sp.gov.br/programa-cata-treco-reforca-limpeza-urbana-e-evita-descarte-irregular-em-poa/";
    private const string SourceText = """
        A Prefeitura de Poá publicou em 24 de julho de 2025 que o programa Cata Treco,
        operado pela Secretaria de Serviços Urbanos, recolhe materiais inservíveis.
        A coleta pode ser solicitada pelo telefone (11) 4636-3270 ou presencialmente
        na sede da pasta, na rua Batatais, nº 30, Jardim Estela. O programa recolhe
        objetos em desuso, como móveis, eletrodomésticos quebrados, madeiras e ferro-velho.
        Os itens devem ser deixados na calçada apenas no dia marcado, conforme o cronograma
        de atendimento por bairro.
        """;

    [Fact]
    public async Task OfficialSourceExtractionCreatesUnpublishedDraftAndRequiresReviewBeforeGroundedPublication()
    {
        // Exercises API contracts with a deterministic test double. Live Azure behavior
        // is covered separately by the --llm-smoke command.
        await using var factory = new ApiFactory();
        await factory.PrepareAsync();
        using var admin = factory.CreateClient();
        using var citizen = factory.CreateClient();

        var login = await Json(await admin.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "operator@example.test",
            senha = "OnlyForIntegration42!"
        }));
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", login.GetProperty("token").GetString());

        var city = "poa-cata-treco-" + Guid.NewGuid().ToString("N")[..8];
        var otherCity = "poa-outro-" + Guid.NewGuid().ToString("N")[..8];
        await Json(await admin.PostAsJsonAsync("/api/v1/municipios", new
        {
            id = city, nome = "Poá (teste de integração)", uf = "SP"
        }));
        await Json(await admin.PostAsJsonAsync("/api/v1/municipios", new
        {
            id = otherCity, nome = "Outro município (teste de isolamento)", uf = "SP"
        }));

        var source = await Json(await admin.PostAsJsonAsync($"/api/v1/municipios/{city}/fontes", new
        {
            titulo = "Programa Cata Treco — Prefeitura Municipal de Poá",
            url = SourceUrl,
            texto = SourceText,
            dataConsulta = DateOnly.FromDateTime(DateTime.UtcNow)
        }));
        var sourceId = source.GetProperty("id").GetGuid();

        var proposal = new ServicoRevisado(
            "cata-treco",
            "Cata Treco",
            "O Cata Treco de Poá recolhe móveis e outros materiais inservíveis. Solicite a coleta pelo telefone (11) 4636-3270 ou presencialmente na Secretaria de Serviços Urbanos. Deixe os itens na calçada apenas no dia marcado para o seu bairro.",
            "Secretaria de Serviços Urbanos de Poá",
            [
                "Solicite a coleta pelo telefone (11) 4636-3270 ou presencialmente na Secretaria de Serviços Urbanos.",
                "Deixe os itens para recolhimento na calçada em frente à residência.",
                "Coloque os objetos na calçada apenas no dia marcado, conforme o cronograma do bairro."
            ],
            [],
            null,
            ["cata treco", "móveis", "sofá", "retirada"]);
        factory.Llm.ExtractionProposal = proposal;

        var draft = await Json(await admin.PostAsync($"/api/v1/fontes/{sourceId}/extracoes", null));
        Assert.False(draft.GetProperty("aprovado").GetBoolean());
        Assert.Equal("Cata Treco", draft.GetProperty("servico").GetProperty("nome").GetString());
        Assert.Contains("(11) 4636-3270", factory.Llm.ExtractionInput ?? string.Empty);

        var draftId = draft.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PostAsync($"/api/v1/rascunhos/{draftId}/publicacao", null)).StatusCode);

        var reviewed = await Json(await admin.PutAsJsonAsync($"/api/v1/rascunhos/{draftId}",
            new RevisarRascunho(proposal, true, draft.GetProperty("revisaoId").GetGuid())));
        Assert.True(reviewed.GetProperty("aprovado").GetBoolean());
        await Json(await admin.PostAsync($"/api/v1/rascunhos/{draftId}/publicacao", null));

        var answer = await Json(await citizen.PostAsJsonAsync($"/api/v1/municipios/{city}/conversas", new
        {
            pergunta = "Como solicito a retirada de um sofá?"
        }));
        Assert.True(answer.GetProperty("informacaoSuficiente").GetBoolean());
        Assert.Contains("(11) 4636-3270", answer.GetProperty("resposta").GetString() ?? string.Empty);
        Assert.Equal("cata-treco", answer.GetProperty("servicosRelacionados")[0].GetString());
        Assert.Equal(SourceUrl, answer.GetProperty("fontes")[0].GetProperty("url").GetString());

        factory.Llm.Insufficient = true;
        var insufficient = await Json(await citizen.PostAsJsonAsync($"/api/v1/municipios/{city}/conversas", new
        {
            pergunta = "Posso solicitar a coleta no sábado?"
        }));
        Assert.False(insufficient.GetProperty("informacaoSuficiente").GetBoolean());
        Assert.Empty(insufficient.GetProperty("fontes").EnumerateArray());
        factory.Llm.Insufficient = false;

        var isolated = await Json(await citizen.PostAsJsonAsync($"/api/v1/municipios/{otherCity}/conversas", new
        {
            pergunta = "Como solicito a retirada de um sofá?"
        }));
        Assert.False(isolated.GetProperty("informacaoSuficiente").GetBoolean());
        Assert.Empty(isolated.GetProperty("fontes").EnumerateArray());

        factory.Llm.Unavailable = true;
        Assert.Equal(HttpStatusCode.ServiceUnavailable,
            (await admin.PostAsync($"/api/v1/fontes/{sourceId}/extracoes", null)).StatusCode);
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return document.RootElement.Clone();
    }
}
