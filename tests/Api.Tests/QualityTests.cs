using System.ComponentModel.DataAnnotations;
using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Domain;
using FacilMunicipio.Api.Security;
using FacilMunicipio.Api.Services;

namespace Api.Tests;

/// <summary>
/// Testes pequenos de regras de domínio e dos limites da API.
/// Não dependem de PostgreSQL, Neo4j nem de serviços externos.
/// </summary>
public class QualityTests
{
    [Theory]
    [InlineData("https://example.org", true)]
    [InlineData("https://example.org/servico?ref=1", true)]
    [InlineData("http://example.org", false)]
    [InlineData("ftp://example.org", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("https://usuario:senha@example.org", false)]
    [InlineData("/caminho-relativo", false)]
    [InlineData("https://", false)]
    public void OfficialLinksNeedAbsoluteHttpsWithoutCredentials(string url, bool expected)
    {
        Assert.Equal(expected, new HttpsUrlAttribute().IsValid(url));
    }

    [Theory]
    [InlineData("CPF 123.456.789-00", "123.456.789-00")]
    [InlineData("CPF 12345678900", "12345678900")]
    [InlineData("E-mail pessoa@example.org", "pessoa@example.org")]
    [InlineData("senha=segredo", "segredo")]
    [InlineData("PASSWORD:teste123", "teste123")]
    public void SensitiveDetailsAreMaskedInQuestions(string input, string secret)
    {
        var sanitized = PrivacyFilter.Clean(input);

        Assert.DoesNotContain(secret, sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[DADO REMOVIDO]", sanitized);
    }

    [Fact]
    public void ConversationHistoryMasksBothSides()
    {
        TrocaResumida[] history =
        [
            new("CPF 123.456.789-00", "email teste@example.org"),
            new("senha=segredo", "PASSWORD:senha123")
        ];

        var sanitized = PrivacyFilter.Clean(history);

        Assert.Equal(2, sanitized.Length);
        Assert.All(sanitized, item =>
        {
            Assert.DoesNotContain("123.456.789-00", item.Pergunta);
            Assert.DoesNotContain("@", item.Resposta);
        });
        Assert.DoesNotContain("segredo", sanitized[1].Pergunta);
        Assert.DoesNotContain("senha123", sanitized[1].Resposta);
    }

    [Fact]
    public void ReviewChangesRevisionAndApprovalState()
    {
        var draft = new Rascunho();
        var firstRevision = draft.RevisaoId;
        var reviewer = Guid.NewGuid();

        draft.Revisar("{\"id\":\"servico\"}", true, reviewer);

        Assert.True(draft.Aprovado);
        Assert.NotEqual(firstRevision, draft.RevisaoId);
        Assert.Equal(reviewer, draft.RevisadoPor);
        Assert.NotNull(draft.RevisadoEm);

        var approvedRevision = draft.RevisaoId;
        draft.Revisar("{}", false, reviewer);

        Assert.False(draft.Aprovado);
        Assert.NotEqual(approvedRevision, draft.RevisaoId);
    }

    [Theory]
    [InlineData("etapa vazia")]
    [InlineData("requisito vazio")]
    [InlineData("termo vazio")]
    [InlineData("etapa longa")]
    public void InvalidServiceDetailsAreRejected(string caseName)
    {
        var steps = caseName == "etapa vazia" ? new[] { "" } :
            caseName == "etapa longa" ? new[] { new string('a', 1001) } :
            new[] { "Verifique o canal municipal." };
        var requirements = caseName == "requisito vazio" ? new[] { " " } : Array.Empty<string>();
        var terms = caseName == "termo vazio" ? new[] { "" } : new[] { "sofá" };
        var service = new ServicoRevisado(
            "cata-treco", "Cata Treco", "Serviço demonstrativo",
            "Secretaria fictícia", steps, requirements, null, terms);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(
            service, new ValidationContext(service), results, validateAllProperties: true);

        Assert.False(valid);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void InsufficientAnswerDoesNotReturnAnyPublishedSource()
    {
        var response = RespostaConversa.Insuficiente("https://example.org/canal");

        Assert.False(response.InformacaoSuficiente);
        Assert.Empty(response.Fontes);
        Assert.Empty(response.ServicosRelacionados);
        Assert.Equal("https://example.org/canal", response.CanalOficialUrl);
    }

    [Fact]
    public async Task DemoRefusesUnrelatedQuestionEvenWithKnowledgeAvailable()
    {
        var responder = new DemoGroundedWiki();
        var service = new ServicoRevisado(
            "cata-treco", "Cata Treco", "Retirada de móveis usados",
            "Secretaria fictícia", ["Consultar o atendimento."], [],
            null, ["sofá"]);
        var evidence = new Conhecimento(service,
            new FonteDto("Fonte de teste", "https://example.org/servicos", null));

        var result = await responder.ResponderAsync(
            "Onde consigo uma segunda via de RG?", [], [evidence], default);

        Assert.False(result.InformacaoSuficiente);
        Assert.Empty(result.ServicoIds);
        Assert.Empty(result.Resposta);
    }

    [Fact]
    public async Task DemoDoesNotAssertUnrecordedOpeningHours()
    {
        var responder = new DemoGroundedWiki();
        var service = new ServicoRevisado(
            "cata-treco", "Cata Treco", "Retirada de móveis usados",
            "Secretaria fictícia", ["Consultar o atendimento."], [],
            null, ["sofá"]);
        var evidence = new Conhecimento(service,
            new FonteDto("Fonte de teste", "https://example.org/servicos", null));

        var result = await responder.ResponderAsync(
            "Qual horário para retirar o sofá?", [], [evidence], default);

        Assert.False(result.InformacaoSuficiente);
        Assert.Empty(result.ServicoIds);
    }

    [Fact]
    public async Task DemoHonorsCancellation()
    {
        var responder = new DemoGroundedWiki();
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => responder.ResponderAsync("Como funciona o Cata Treco?",
                [], Array.Empty<Conhecimento>(), canceled.Token));
    }
}
