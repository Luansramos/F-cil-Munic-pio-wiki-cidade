using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Services;

namespace Api.Tests;

public class DemoGroundedWikiTests
{
    private static readonly Conhecimento Evidence = new(
        new ServicoRevisado("cata-treco", "Cata Treco",
            "Retirada de objetos grandes mediante orientação do município.",
            "Órgão de demonstração",
            ["Conferir os itens aceitos.", "Consultar o canal oficial."],
            [], null, ["sofá", "móveis"]),
        new FonteDto("Fonte fictícia para testes", "https://example.org/servico", "2026-11-01"));

    [Fact]
    public async Task RespondsOnlyWithReviewedEvidence()
    {
        var demo = new DemoGroundedWiki();
        var answer = await demo.ResponderAsync("Como solicito a retirada de um sofá?", [], [Evidence], default);
        Assert.True(answer.InformacaoSuficiente);
        Assert.Equal(["cata-treco"], answer.ServicoIds);
        Assert.Contains("Conferir os itens aceitos.", answer.Resposta);
        Assert.DoesNotContain("sábado", answer.Resposta, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Posso retirar um sofá no sábado?")]
    [InlineData("Posso levar lixo eletrônico no sábado?")]
    [InlineData("Qual o valor da coleta de sofá?")]
    public async Task AbstainsWhenEvidenceDoesNotSupportDetail(string question)
    {
        var demo = new DemoGroundedWiki();
        var answer = await demo.ResponderAsync(question, [], [Evidence], default);
        Assert.False(answer.InformacaoSuficiente);
        Assert.Empty(answer.ServicoIds);
        Assert.Equal("", answer.Resposta);
    }

    [Fact]
    public async Task ShortFollowUpUsesLastPublishedServiceAsContext()
    {
        var demo = new DemoGroundedWiki();
        var service = Evidence.Servico with
        {
            Etapas = ["Para esta demonstração fictícia, consulte o atendimento de domingo."]
        };
        var evidence = Evidence with { Servico = service };
        var previous = new TrocaResumida(
            "Como solicito a retirada de um sofá?",
            "Cata Treco: orientações sobre a retirada de objetos.");

        var withoutHistory = await demo.ResponderAsync(
            "E no domingo?", [], [evidence], default);
        var withHistory = await demo.ResponderAsync(
            "E no domingo?", [previous], [evidence], default);

        Assert.False(withoutHistory.InformacaoSuficiente);
        Assert.True(withHistory.InformacaoSuficiente);
        Assert.Equal(["cata-treco"], withHistory.ServicoIds);
        Assert.Contains("domingo", withHistory.Resposta, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FollowUpDoesNotInventOpeningDayWhenUnrecorded()
    {
        var demo = new DemoGroundedWiki();
        var history = new[]
        {
            new TrocaResumida("Como solicito a retirada de um sofá?",
                "Cata Treco: consulte o canal oficial.")
        };

        var answer = await demo.ResponderAsync(
            "E no domingo?", history, [Evidence], default);

        Assert.False(answer.InformacaoSuficiente);
        Assert.Empty(answer.ServicoIds);
    }

    [Fact]
    public async Task HistoryCannotReferenceServiceAbsentFromCurrentMunicipality()
    {
        var demo = new DemoGroundedWiki();
        var otherMunicipalityService = new ServicoRevisado(
            "iluminacao-publica", "Iluminação pública",
            "Atendimento a postes de iluminação com lâmpadas apagadas.",
            null, ["Atendimento de domingo deve ser confirmado."], [],
            null, ["poste", "lampada"]);
        var context = new Conhecimento(otherMunicipalityService,
            new FonteDto("Fonte fictícia do segundo município",
                "https://example.org/iluminacao", null));
        var history = new[]
        {
            new TrocaResumida("Como solicito retirada de sofá?",
                "Cata Treco: orientações sobre móveis.")
        };

        var answer = await demo.ResponderAsync(
            "E no domingo?", history, [context], default);

        Assert.False(answer.InformacaoSuficiente);
        Assert.Empty(answer.ServicoIds);
    }

    [Fact]
    public async Task DemoReturnsPlainTextNumberedSteps()
    {
        var service = Evidence.Servico with { Requisitos = ["Requisito fictício de teste."] };
        var demo = new DemoGroundedWiki();

        var answer = await demo.ResponderAsync(
            "Como solicito a retirada de um sofá?", [],
            [Evidence with { Servico = service }], default);

        Assert.True(answer.InformacaoSuficiente);
        Assert.Contains("1. Conferir os itens aceitos.", answer.Resposta);
        Assert.Contains("1. Requisito fictício de teste.", answer.Resposta);
        Assert.DoesNotContain("**", answer.Resposta);
        Assert.DoesNotContain("\n- ", answer.Resposta);
    }

    [Fact]
    public async Task DoesNotPretendToExtractWithALlm()
    {
        var demo = new DemoGroundedWiki();
        await Assert.ThrowsAsync<LlmUnavailableException>(() => demo.ExtrairAsync("Texto fictício.", default));
    }
}
