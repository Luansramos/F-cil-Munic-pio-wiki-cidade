using System.Text.Json;
using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Services;

namespace Api.Tests;

public sealed class AzureOpenAiWikiTests
{
    [Fact]
    public async Task ExtractsAndCleansAReviewableProposal()
    {
        var client = new FakeCompletionClient(ExtractionJson(
            nome: "  Emissão de Certidão  ",
            descricao: "  Solicite a certidão no portal municipal.  ",
            orgao: "  Secretaria de Atendimento ",
            etapas: ["  Acesse o portal ", "Envie o pedido  "],
            requisitos: [" Documento de identidade "],
            canalUrl: " https://cidade.gov.br/certidao ",
            termos: [" certidão ", " emissão "]));

        var result = await new AzureOpenAiWiki(client).ExtrairAsync("Fonte da prefeitura", CancellationToken.None);

        Assert.Equal("emissao-de-certidao", result.Id);
        Assert.Equal("Emissão de Certidão", result.Nome);
        Assert.Equal("Solicite a certidão no portal municipal.", result.Descricao);
        Assert.Equal("Secretaria de Atendimento", result.Orgao);
        Assert.Equal(["Acesse o portal", "Envie o pedido"], result.Etapas);
        Assert.Equal(["Documento de identidade"], result.Requisitos);
        Assert.Equal("https://cidade.gov.br/certidao", result.CanalUrl);
        Assert.Equal(["certidão", "emissão"], result.Termos);
        Assert.Equal("servico_municipal_proposta", client.SchemaName);
        Assert.Equal(1400, client.MaxOutputTokens);
    }

    [Fact]
    public async Task ExtractsMissingOptionalDataAsNullAndEmptyLists()
    {
        var client = new FakeCompletionClient(ExtractionJson(
            nome: "Alvará de funcionamento",
            descricao: "Solicite o alvará no setor municipal.",
            orgao: " ", etapas: [], requisitos: [], canalUrl: "", termos: []));

        var result = await new AzureOpenAiWiki(client).ExtrairAsync("Texto publicado", CancellationToken.None);

        Assert.Null(result.Orgao);
        Assert.Null(result.CanalUrl);
        Assert.Empty(result.Etapas);
        Assert.Empty(result.Requisitos);
        Assert.Empty(result.Termos);
    }

    [Fact]
    public async Task ConvertsNullArraysReturnedByTheModelToEmptyLists()
    {
        var client = new FakeCompletionClient(
            """{"nome":"Alvará municipal","descricao":"Solicite o alvará no setor municipal.","orgao":null,"etapas":null,"requisitos":null,"canalUrl":null,"termos":null}""");

        var result = await new AzureOpenAiWiki(client).ExtrairAsync("Fonte", CancellationToken.None);

        Assert.Empty(result.Etapas);
        Assert.Empty(result.Requisitos);
        Assert.Empty(result.Termos);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RejectsBlankSourceWithoutCallingProvider(string source)
    {
        var client = new FakeCompletionClient(ExtractionJson());

        await Assert.ThrowsAsync<LlmUnavailableException>(
            () => new AzureOpenAiWiki(client).ExtrairAsync(source, CancellationToken.None));

        Assert.Equal(0, client.CallCount);
    }

    [Fact]
    public async Task RejectsSourceOverThirtyThousandCharactersWithoutCallingProvider()
    {
        var client = new FakeCompletionClient(ExtractionJson());

        await Assert.ThrowsAsync<LlmUnavailableException>(() =>
            new AzureOpenAiWiki(client).ExtrairAsync(new string('x', 30001), CancellationToken.None));

        Assert.Equal(0, client.CallCount);
    }

    [Theory]
    [InlineData(" ", "Descrição válida", null, "https://cidade.gov.br")]
    [InlineData("Serviço", "", null, "https://cidade.gov.br")]
    [InlineData("Serviço", "Descrição válida", null, "http://cidade.gov.br")]
    [InlineData("Serviço", "Descrição válida", null, "not-a-url")]
    public async Task RejectsModelOutputThatViolatesServiceValidation(
        string name, string description, string? department, string? channelUrl)
    {
        var client = new FakeCompletionClient(ExtractionJson(
            nome: name, descricao: description, orgao: department, canalUrl: channelUrl));

        await Assert.ThrowsAsync<LlmUnavailableException>(() =>
            new AzureOpenAiWiki(client).ExtrairAsync("Fonte", CancellationToken.None));
    }

    [Fact]
    public async Task RejectsExtractionWithMoreThanTwentySteps()
    {
        var client = new FakeCompletionClient(ExtractionJson(
            etapas: Enumerable.Range(1, 21).Select(index => $"Etapa {index}").ToArray()));

        await Assert.ThrowsAsync<LlmUnavailableException>(() =>
            new AzureOpenAiWiki(client).ExtrairAsync("Fonte", CancellationToken.None));
    }

    [Fact]
    public async Task RejectsExtractionWithTooManyTermsOrOverlongTerm()
    {
        var tooManyTerms = new FakeCompletionClient(ExtractionJson(termos: Enumerable.Range(1, 31)
            .Select(index => $"termo-{index}").ToArray()));
        var longTerm = new FakeCompletionClient(ExtractionJson(termos: [new string('x', 101)]));

        await Assert.ThrowsAsync<LlmUnavailableException>(() =>
            new AzureOpenAiWiki(tooManyTerms).ExtrairAsync("Fonte", CancellationToken.None));
        await Assert.ThrowsAsync<LlmUnavailableException>(() =>
            new AzureOpenAiWiki(longTerm).ExtrairAsync("Fonte", CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-json")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task RejectsEmptyOrMalformedExtractionCompletion(string output)
    {
        var client = new FakeCompletionClient(output);

        await Assert.ThrowsAsync<LlmUnavailableException>(() =>
            new AzureOpenAiWiki(client).ExtrairAsync("Fonte", CancellationToken.None));
    }

    [Fact]
    public async Task WrapsProviderFailureWithoutExposingItsMessage()
    {
        var providerFailure = new InvalidOperationException("secret endpoint and key details");
        var client = new FakeCompletionClient(_ => throw providerFailure);

        var error = await Assert.ThrowsAsync<LlmUnavailableException>(() =>
            new AzureOpenAiWiki(client).ExtrairAsync("Fonte", CancellationToken.None));

        Assert.Same(providerFailure, error.InnerException);
        Assert.DoesNotContain("secret", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PropagatesCancellationFromProvider()
    {
        using var source = new CancellationTokenSource();
        var client = new FakeCompletionClient(_ => throw new OperationCanceledException(source.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new AzureOpenAiWiki(client).ExtrairAsync("Fonte", source.Token));
    }

    [Fact]
    public async Task AnswersFromPublishedEvidenceAndTrimsTheAnswer()
    {
        var client = new FakeCompletionClient(AnswerJson(
            "  Solicite a certidão no portal municipal.  ", ["certidao-municipal"], true,
            [("certidao-municipal", "Solicite a certidão no portal municipal.")]));

        var result = await new AzureOpenAiWiki(client).ResponderAsync(
            "Como solicito a certidão?", [], [Knowledge()], CancellationToken.None);

        Assert.True(result.InformacaoSuficiente);
        Assert.Equal("Solicite a certidão no portal municipal.", result.Resposta);
        Assert.Equal(["certidao-municipal"], result.ServicoIds);
        Assert.Equal("resposta_fundamentada", client.SchemaName);
        Assert.Equal(1000, client.MaxOutputTokens);
    }

    [Fact]
    public async Task AcceptsEvidenceCopiedAcrossAccentAndWhitespaceDifferences()
    {
        var client = new FakeCompletionClient(AnswerJson(
            "A solicitação é feita no portal.", ["certidao-municipal"], true,
            [("certidao-municipal", "SOLICITE A certidao   no portal municipal.")]));

        var result = await new AzureOpenAiWiki(client).ResponderAsync(
            "Como faço?", [], [Knowledge()], CancellationToken.None);

        Assert.True(result.InformacaoSuficiente);
    }

    [Fact]
    public async Task AcceptsEvidenceCopiedFromARequirement()
    {
        var client = new FakeCompletionClient(AnswerJson(
            "Apresente a identificação.", ["certidao-municipal"], true,
            [("certidao-municipal", "Documento de identidade")]));

        var result = await new AzureOpenAiWiki(client).ResponderAsync(
            "Quais documentos?", [], [Knowledge()], CancellationToken.None);

        Assert.True(result.InformacaoSuficiente);
    }

    [Fact]
    public async Task AcceptsMultipleCitedServicesWhenEachHasEvidence()
    {
        var second = Knowledge("alvara-comercial", "Alvará comercial",
            "Solicite o alvará comercial no setor municipal.");
        var client = new FakeCompletionClient(AnswerJson(
            "Consulte os dois setores municipais.", ["certidao-municipal", "alvara-comercial"], true,
            [
                ("certidao-municipal", "Solicite a certidão no portal municipal."),
                ("alvara-comercial", "Solicite o alvará comercial no setor municipal.")
            ]));

        var result = await new AzureOpenAiWiki(client).ResponderAsync(
            "Quais serviços existem?", [], [Knowledge(), second], CancellationToken.None);

        Assert.True(result.InformacaoSuficiente);
        Assert.Equal(["certidao-municipal", "alvara-comercial"], result.ServicoIds);
    }

    [Fact]
    public async Task SendsOnlyPublishedFieldsAndTheLastThreeHistoryExchanges()
    {
        var client = new FakeCompletionClient(AnswerJson(
            "Solicite a certidão no portal municipal.", ["certidao-municipal"], true,
            [("certidao-municipal", "Solicite a certidão no portal municipal.")]));
        var history = Enumerable.Range(1, 4)
            .Select(index => new TrocaResumida($"pergunta-{index}", $"resposta-{index}"))
            .ToArray();
        var knowledge = Knowledge() with
        {
            Fonte = new FonteDto("Fonte privada", "https://fonte.interna.gov/segredo", "2026-01-01"),
            Servico = Knowledge().Servico with { CanalUrl = "https://canal.gov.br/privado" }
        };

        await new AzureOpenAiWiki(client).ResponderAsync(
            "Pergunta atual", history, [knowledge], CancellationToken.None);

        Assert.NotNull(client.UserPayload);
        Assert.Contains("pergunta-2", client.UserPayload);
        Assert.Contains("pergunta-4", client.UserPayload);
        Assert.DoesNotContain("pergunta-1", client.UserPayload);
        Assert.DoesNotContain("https://", client.UserPayload);
        Assert.DoesNotContain("Fonte privada", client.UserPayload);
        Assert.Contains("Secretaria de Atendimento", client.UserPayload);
        Assert.Contains("Documento de identidade", client.UserPayload);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReturnsInsufficientForBlankQuestionWithoutCallingProvider(string question)
    {
        var client = new FakeCompletionClient(AnswerJson());

        var result = await new AzureOpenAiWiki(client).ResponderAsync(
            question, [], [Knowledge()], CancellationToken.None);

        AssertInsufficient(result);
        Assert.Equal(0, client.CallCount);
    }

    [Fact]
    public async Task ReturnsInsufficientForEmptyContextWithoutCallingProvider()
    {
        var client = new FakeCompletionClient(AnswerJson());

        var result = await new AzureOpenAiWiki(client).ResponderAsync(
            "Como solicito?", [], [], CancellationToken.None);

        AssertInsufficient(result);
        Assert.Equal(0, client.CallCount);
    }

    [Theory]
    [InlineData("", new[] { "certidao-municipal" }, true, "certidao-municipal", "Solicite a certidão no portal municipal.")]
    [InlineData("resposta", new string[0], true, "certidao-municipal", "Solicite a certidão no portal municipal.")]
    [InlineData("resposta", new[] { "certidao-municipal" }, false, "certidao-municipal", "Solicite a certidão no portal municipal.")]
    [InlineData("resposta", new[] { "certidao-municipal" }, true, "certidao-municipal", "")]
    [InlineData("resposta", new[] { "certidao-municipal" }, true, "", "Solicite a certidão no portal municipal.")]
    public async Task ReturnsInsufficientWhenRequiredAnswerFieldsAreAbsentOrFalse(
        string answer, string[] ids, bool sufficient, string evidenceId, string quote)
    {
        var client = new FakeCompletionClient(AnswerJson(
            answer, ids, sufficient, [(evidenceId, quote)]));

        var result = await new AzureOpenAiWiki(client).ResponderAsync(
            "Pergunta", [], [Knowledge()], CancellationToken.None);

        AssertInsufficient(result);
    }

    [Fact]
    public async Task ReturnsInsufficientWhenEvidenceListIsEmpty()
    {
        var client = new FakeCompletionClient(AnswerJson(
            "resposta", ["certidao-municipal"], true, []));

        var result = await new AzureOpenAiWiki(client).ResponderAsync(
            "Pergunta", [], [Knowledge()], CancellationToken.None);

        AssertInsufficient(result);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("null")]
    [InlineData("{")]
    public async Task RejectsMalformedAnswerCompletion(string output)
    {
        var client = new FakeCompletionClient(output);

        await Assert.ThrowsAsync<LlmUnavailableException>(() =>
            new AzureOpenAiWiki(client).ResponderAsync("Pergunta", [], [Knowledge()], CancellationToken.None));
    }

    [Theory]
    [InlineData("answer", "https://cidade.gov.br")]
    [InlineData("answer", "http://cidade.gov.br/servico")]
    public async Task RejectsAnswerContainingUrls(string answer, string url)
    {
        var client = new FakeCompletionClient(AnswerJson(
            $"{answer} {url}", ["certidao-municipal"], true,
            [("certidao-municipal", "Solicite a certidão no portal municipal.")]));

        var result = await new AzureOpenAiWiki(client).ResponderAsync(
            "Pergunta", [], [Knowledge()], CancellationToken.None);

        AssertInsufficient(result);
    }

    [Theory]
    [InlineData(new[] { "certidao-municipal", "certidao-municipal" }, "certidao-municipal")]
    [InlineData(new[] { "nao-publicado" }, "nao-publicado")]
    public async Task RejectsDuplicateOrUnknownServiceIds(string[] ids, string evidenceId)
    {
        var client = new FakeCompletionClient(AnswerJson(
            "Resposta", ids, true, [(evidenceId, "Solicite a certidão no portal municipal.")]));

        var result = await new AzureOpenAiWiki(client).ResponderAsync(
            "Pergunta", [], [Knowledge()], CancellationToken.None);

        AssertInsufficient(result);
    }

    [Theory]
    [InlineData("outro-servico", "Solicite a certidão no portal municipal.")]
    [InlineData("certidao-municipal", "Evidência inventada sobre o serviço.")]
    [InlineData("certidao-municipal", "CPF")]
    [InlineData("certidao-municipal", "emissão de certidão municipal")]
    public async Task RejectsUnknownShortFabricatedOrNonFactualEvidence(string serviceId, string quote)
    {
        var client = new FakeCompletionClient(AnswerJson(
            "Resposta", ["certidao-municipal"], true, [(serviceId, quote)]));

        var result = await new AzureOpenAiWiki(client).ResponderAsync(
            "Pergunta", [], [Knowledge()], CancellationToken.None);

        AssertInsufficient(result);
    }

    [Fact]
    public async Task RequiresEvidenceForEveryCitedService()
    {
        var second = Knowledge("alvara-comercial", "Alvará comercial",
            "Solicite o alvará comercial no setor municipal.");
        var client = new FakeCompletionClient(AnswerJson(
            "Resposta", ["certidao-municipal", "alvara-comercial"], true,
            [("certidao-municipal", "Solicite a certidão no portal municipal.")]));

        var result = await new AzureOpenAiWiki(client).ResponderAsync(
            "Pergunta", [], [Knowledge(), second], CancellationToken.None);

        AssertInsufficient(result);
    }

    [Fact]
    public async Task RejectsEmptyProviderCompletion()
    {
        var client = new FakeCompletionClient(" ");

        await Assert.ThrowsAsync<LlmUnavailableException>(() =>
            new AzureOpenAiWiki(client).ResponderAsync("Pergunta", [], [Knowledge()], CancellationToken.None));
    }

    [Fact]
    public async Task PropagatesAnswerCancellationFromProvider()
    {
        using var source = new CancellationTokenSource();
        var client = new FakeCompletionClient(_ => throw new OperationCanceledException(source.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new AzureOpenAiWiki(client).ResponderAsync("Pergunta", [], [Knowledge()], source.Token));
    }

    private static Conhecimento Knowledge(
        string id = "certidao-municipal",
        string name = "Certidão municipal",
        string description = "Solicite a certidão no portal municipal.")
        => new(
            new ServicoRevisado(
                id, name, description, "Secretaria de Atendimento",
                ["Acesse o portal municipal"], ["Documento de identidade"],
                "https://cidade.gov.br/certidao", ["certidão", "emissão"]),
            new FonteDto("Portal da prefeitura", "https://cidade.gov.br/fonte", "2026-01-01"));

    private static string ExtractionJson(
        string nome = "Certidão municipal",
        string descricao = "Solicite a certidão no portal municipal.",
        string? orgao = "Secretaria de Atendimento",
        string[]? etapas = null,
        string[]? requisitos = null,
        string? canalUrl = null,
        string[]? termos = null)
        => JsonSerializer.Serialize(new
        {
            nome,
            descricao,
            orgao,
            etapas = etapas ?? ["Acesse o portal"],
            requisitos = requisitos ?? ["Documento de identidade"],
            canalUrl,
            termos = termos ?? ["certidão"]
        });

    private static string AnswerJson(
        string resposta = "Solicite a certidão no portal municipal.",
        string[]? ids = null,
        bool informacaoSuficiente = true,
        (string ServiceId, string Quote)[]? evidence = null)
        => JsonSerializer.Serialize(new
        {
            resposta,
            servicoIds = ids ?? ["certidao-municipal"],
            informacaoSuficiente,
            evidencias = (evidence ?? [("certidao-municipal", "Solicite a certidão no portal municipal.")])
                .Select(item => new { servicoId = item.ServiceId, trecho = item.Quote })
        });

    private static void AssertInsufficient(ResultadoLlm result)
    {
        Assert.False(result.InformacaoSuficiente);
        Assert.Empty(result.Resposta);
        Assert.Empty(result.ServicoIds);
    }

    private sealed class FakeCompletionClient : IAzureOpenAiJsonCompletionClient
    {
        private readonly Func<CancellationToken, string?> _completion;

        public FakeCompletionClient(string? completion)
            : this(_ => completion) { }

        public FakeCompletionClient(Func<CancellationToken, string?> completion) => _completion = completion;

        public int CallCount { get; private set; }
        public string? SystemPrompt { get; private set; }
        public string? UserPayload { get; private set; }
        public string? SchemaName { get; private set; }
        public string? Schema { get; private set; }
        public int MaxOutputTokens { get; private set; }

        public Task<string?> CompleteAsync(
            string systemPrompt,
            string userPayload,
            string schemaName,
            string schema,
            int maxOutputTokens,
            CancellationToken ct)
        {
            CallCount++;
            SystemPrompt = systemPrompt;
            UserPayload = userPayload;
            SchemaName = schemaName;
            Schema = schema;
            MaxOutputTokens = maxOutputTokens;
            return Task.FromResult(_completion(ct));
        }
    }
}
