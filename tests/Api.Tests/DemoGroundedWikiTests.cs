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
    public async Task DoesNotPretendToExtractWithALlm()
    {
        var demo = new DemoGroundedWiki();
        await Assert.ThrowsAsync<LlmUnavailableException>(() => demo.ExtrairAsync("Texto fictício.", default));
    }
}
