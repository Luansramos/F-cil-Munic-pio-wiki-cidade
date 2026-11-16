using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Domain;
using FacilMunicipio.Api.Security;
using FacilMunicipio.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
namespace Api.Tests;

/// <summary>Testes automatizados de Cybersecurity. Usam apenas dados simulados; nenhuma chamada a LLM real.</summary>
public class SecurityTests
{
    private const string Cpf = "123.456.789-00";
    private static readonly string[] Originals = [Cpf, "12345678900", "maria@example.org", "Abc12345", "12.345.678-9", "diabetes"];

    // ---------- 1. Parsing de PII ----------

    [Theory]
    [InlineData("Meu CPF é 123.456.789-00", PiiCategory.Cpf, "123.456.789-00")]
    [InlineData("CPF 12345678900", PiiCategory.Cpf, "12345678900")]
    [InlineData("o cpf: 123456789-00", PiiCategory.Cpf, "123456789-00")]
    [InlineData("Meu RG é 12.345.678-9", PiiCategory.Rg, "12.345.678-9")]
    [InlineData("identidade número 12345678", PiiCategory.Rg, "12345678")]
    [InlineData("escreva para maria@example.org", PiiCategory.Email, "maria@example.org")]
    [InlineData("senha=Abc12345", PiiCategory.Senha, "Abc12345")]
    [InlineData("PASSWORD: Abc12345", PiiCategory.Senha, "Abc12345")]
    [InlineData("minha senha é Abc12345", PiiCategory.Senha, "Abc12345")]
    [InlineData("Eu tenho diabetes e preciso de ajuda", PiiCategory.Saude, "diabetes")]
    [InlineData("fui diagnosticada com depressão", PiiCategory.Saude, "depressão")]
    [InlineData("meu laudo é CID F32", PiiCategory.Saude, "CID F32")]
    public void DetectsCategoriesAndFormats(string input, PiiCategory category, string secret)
    {
        var scan = PrivacyFilter.Analyze(input, "conversa.pergunta");
        Assert.Contains(scan.Findings, f => f.Category == category && f.Path == "conversa.pergunta");
        Assert.DoesNotContain(secret, scan.Sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(PrivacyFilter.Mask, scan.Sanitized);
        Assert.Equal("SUBSTITUIDO", scan.Action);
    }

    [Theory]
    [InlineData("Como alterar minha senha?")]
    [InlineData("Onde fica o hospital municipal?")]
    [InlineData("Qual documento preciso levar para a retirada de sofá?")]
    [InlineData("Como pedir a segunda via do RG?")]
    [InlineData("Qual o horário do posto de saúde?")]
    [InlineData("Meu telefone é 11999998888")]
    [InlineData("O protocolo 2026000123 foi aberto")]
    [InlineData("Há atendimento para quem tem diabetes no posto?")]
    public void NormalQuestionsAreNotFlagged(string input)
    {
        var scan = PrivacyFilter.Analyze(input, "conversa.pergunta");
        Assert.False(scan.HasPii);
        Assert.Equal(input, scan.Sanitized);
        Assert.Equal("NENHUMA", scan.Action);
    }

    [Fact]
    public void KnownLimitations_AreDocumentedByTests()
    {
        // Falso negativo: CPF por extenso ou fragmentado não é detectado por regex.
        Assert.False(PrivacyFilter.Analyze("meu cpf é um dois três quatro cinco", "x").HasPii);
        Assert.False(PrivacyFilter.Analyze("CPF 123 456 789 00", "x").HasPii);
        // Falso positivo: frase com padrão de senha sem ser uma senha.
        Assert.True(PrivacyFilter.Analyze("minha senha é importante", "x").HasPii);
    }

    [Fact]
    public void ScanObjectsDoNotLeakOriginalsThroughToString()
    {
        var scan = PrivacyFilter.Analyze($"CPF {Cpf} e senha=Abc12345", "conversa.pergunta");
        var text = scan + " " + string.Join(" ", scan.Marks.Select(m => m.ToString())) + " " + string.Join(" ", scan.Findings);
        Assert.All(new[] { "123.456", "12345678900", "Abc12345" }, secret => Assert.DoesNotContain(secret, text));
        // O valor é interno ao tipo: nem a serialização acidental o expõe.
        Assert.DoesNotContain("Abc12345", JsonSerializer.Serialize(scan.Marks));
    }

    [Fact]
    public void LegacyCleanKeepsExistingBehaviourForHistory()
    {
        var cleaned = PrivacyFilter.Clean([new TrocaResumida($"CPF {Cpf}", "email maria@example.org")]);
        Assert.DoesNotContain("123.456", cleaned[0].Pergunta); Assert.DoesNotContain("@", cleaned[0].Resposta);
    }

    // ---------- 2/3. Entradas, políticas e sinks na conversa ----------

    [Fact]
    public async Task Conversation_SanitizesQuestionAndBothSidesOfHistoryBeforeTheLlm_AndAuditsOnlyMetadata()
    {
        await using var f = new SecurityFactory(); await f.SeedAsync();
        using var client = f.Client();
        var body = new EnvioPergunta($"E se eu tiver o CPF {Cpf}?", [new("Meu email é maria@example.org", "Anotei senha=Abc12345"), new("Tenho diabetes", "Entendi, RG 12.345.678-9")]);
        var response = await client.PostAsJsonAsync($"/api/v1/municipios/{SecurityFactory.CityA}/conversas", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var llmView = f.Llm.Question + JsonSerializer.Serialize(f.Llm.History);
        Assert.All(Originals.Where(o => o != "12345678900"), secret => Assert.DoesNotContain(secret, llmView, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(PrivacyFilter.Mask, f.Llm.Question); Assert.Equal(2, f.Llm.History.Length);

        var events = await f.WithDb(db => db.EventosSeguranca.AsNoTracking().ToListAsync());
        Assert.Contains(events, e => e.Origem == "conversa.pergunta" && e.Categoria == PiiCategory.Cpf);
        Assert.Contains(events, e => e.Origem == "conversa.historico[0].pergunta" && e.Categoria == PiiCategory.Email);
        Assert.Contains(events, e => e.Origem == "conversa.historico[0].resposta" && e.Categoria == PiiCategory.Senha);
        Assert.Contains(events, e => e.Origem == "conversa.historico[1].pergunta" && e.Categoria == PiiCategory.Saude);
        Assert.Contains(events, e => e.Origem == "conversa.historico[1].resposta" && e.Categoria == PiiCategory.Rg);
        Assert.All(events, e =>
        {
            Assert.Equal(SecurityFactory.CityA, e.MunicipioId); Assert.Equal(AcaoProtecao.Sanitizado, e.Acao); Assert.Equal(SinkDestino.Llm, e.Destino);
            Assert.Equal(TraducaoEstado.Pendente, e.TraducaoEstado); Assert.Equal(events[0].CorrelacaoId, e.CorrelacaoId);
        });
        var dump = JsonSerializer.Serialize(events);
        Assert.All(Originals, secret => Assert.DoesNotContain(secret, dump, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("senha=", dump, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Conversation_TaintPropagatesAcrossFields()
    {
        await using var f = new SecurityFactory(); await f.SeedAsync();
        using var client = f.Client();
        // O valor da senha aparece solto no histórico, sem "senha=": a marca da pergunta o remove também lá.
        var body = new EnvioPergunta("minha senha é Abc12345", [new("Repita: Abc12345", "ok Abc12345")]);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/v1/municipios/{SecurityFactory.CityA}/conversas", body)).StatusCode);
        Assert.DoesNotContain("Abc12345", f.Llm.Question + JsonSerializer.Serialize(f.Llm.History));
    }

    [Fact]
    public async Task Conversation_NormalQuestionsPassUnchangedWithoutEvents()
    {
        await using var f = new SecurityFactory(); await f.SeedAsync();
        using var client = f.Client();
        var question = "Como alterar minha senha?";
        var response = await client.PostAsJsonAsync($"/api/v1/municipios/{SecurityFactory.CityA}/conversas", new EnvioPergunta(question, [new("Onde fica o hospital municipal?", "Consulte o canal oficial.")]));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("informacaoSuficiente").GetBoolean());
        Assert.Equal(question, f.Llm.Question); Assert.Equal("Onde fica o hospital municipal?", f.Llm.History[0].Pergunta);
        Assert.Empty(await f.WithDb(db => db.EventosSeguranca.ToListAsync()));
    }

    [Fact]
    public async Task Conversation_AnalyzesBeforeEarlyReturn_ButNeverForUnknownCity()
    {
        await using var f = new SecurityFactory(); await f.SeedAsync(publishedA: false);
        using var client = f.Client();
        var unpublished = await client.PostAsJsonAsync($"/api/v1/municipios/{SecurityFactory.CityA}/conversas", new EnvioPergunta($"CPF {Cpf}", null));
        Assert.False((await unpublished.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("informacaoSuficiente").GetBoolean());
        Assert.Single(await f.WithDb(db => db.EventosSeguranca.ToListAsync()));
        var unknown = await client.PostAsJsonAsync("/api/v1/municipios/nao-existe/conversas", new EnvioPergunta($"CPF {Cpf}", null));
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Single(await f.WithDb(db => db.EventosSeguranca.ToListAsync()));
    }

    [Theory]
    [InlineData("Para falar com a prefeitura use CPF 111.444.777-35.")]          // PII nova gerada pela LLM
    [InlineData("Seu documento 123 456 789 00 foi anotado.")]                     // valor marcado, reformatado (taint)
    [InlineData("Escreva para atendimento.pessoa@example.org.")]
    public async Task Conversation_BlocksPiiGeneratedByTheLlmAndReturnsSafeResponse(string unsafeReply)
    {
        await using var f = new SecurityFactory(); await f.SeedAsync();
        f.Llm.Reply = unsafeReply; using var client = f.Client();
        var response = await client.PostAsJsonAsync($"/api/v1/municipios/{SecurityFactory.CityA}/conversas", new EnvioPergunta($"Meu CPF é {Cpf}", null));
        var text = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(JsonDocument.Parse(text).RootElement.GetProperty("informacaoSuficiente").GetBoolean());
        Assert.DoesNotContain("111.444", text); Assert.DoesNotContain("123 456", text); Assert.DoesNotContain("@example.org", text);
        var blocked = await f.WithDb(db => db.EventosSeguranca.Where(e => e.Acao == AcaoProtecao.Bloqueado).ToListAsync());
        Assert.Contains(blocked, e => e.Destino == SinkDestino.RespostaPublica && e.Origem == "llm.resposta" && e.Codigo == "SINK_BLOQUEADO");
    }

    [Fact]
    public async Task Logs_NeverContainOriginalValues_EvenWhenSanitizingAndBlocking()
    {
        await using var f = new SecurityFactory(); await f.SeedAsync();
        f.Llm.Reply = "Seu documento 123 456 789 00 foi anotado."; using var client = f.Client();
        await client.PostAsJsonAsync($"/api/v1/municipios/{SecurityFactory.CityA}/conversas", new EnvioPergunta($"Meu CPF é {Cpf}, senha=Abc12345, maria@example.org", null));
        var logs = f.Logs.All;
        Assert.Contains("PII sanitizada", logs); Assert.Contains("Saída bloqueada", logs);
        Assert.All(Originals.Append("123 456 789 00"), secret => Assert.DoesNotContain(secret, logs, StringComparison.OrdinalIgnoreCase));
    }

    // ---------- 3. Camada de sinks (unitário, com taint explícito) ----------

    [Fact]
    public async Task SinkGuard_BlocksTaintedValueWithoutTreatment_AndAllowsSanitizedContent()
    {
        await using var f = new SecurityFactory(); await f.SeedAsync();
        using var scope = f.Services.CreateScope(); var guard = scope.ServiceProvider.GetRequiredService<SinkGuard>();
        var safe = await guard.SanitizeAsync(SecurityFactory.CityA, $"cpf {Cpf} e senha=Abc12345", "conversa.pergunta", SinkDestino.Llm);
        // Tratado: segue. Bruto (ou transformado) com a marca: bloqueado.
        Assert.True(await guard.VerifyAsync(SecurityFactory.CityA, SinkDestino.Llm, ("conversa.pergunta", safe)));
        Assert.False(await guard.VerifyAsync(SecurityFactory.CityA, SinkDestino.Llm, ("x.raw", $"cpf {Cpf}")));
        Assert.False(await guard.VerifyAsync(SecurityFactory.CityA, SinkDestino.RespostaPublica, ("x.transformado", "valor: 12345678900")));
        Assert.False(await guard.VerifyAsync(SecurityFactory.CityA, SinkDestino.Persistencia, ("x.upper", "ABC12345 no texto")));
        Assert.Equal(SinkGuard.RejectionMessage, (await Assert.ThrowsAsync<ApiException>(() =>
            guard.RequireCleanAsync(SecurityFactory.CityA, SinkDestino.Grafo, [("rascunho.descricao", "Fale com maria@example.org")]))).Message);
    }

    [Fact]
    public async Task SinkGuard_LogIsBlockedWhenMessageHasMarkedData()
    {
        await using var f = new SecurityFactory(); await f.SeedAsync();
        using var scope = f.Services.CreateScope(); var guard = scope.ServiceProvider.GetRequiredService<SinkGuard>();
        await guard.SanitizeAsync(SecurityFactory.CityA, "senha=Abc12345", "conversa.pergunta", SinkDestino.Llm);
        guard.Log(LogLevel.Information, "tentativa com Abc12345 e CPF 123.456.789-00");
        guard.Log(LogLevel.Information, "mensagem sem dado pessoal");
        Assert.Contains("Log bloqueado", f.Logs.All); Assert.Contains("mensagem sem dado pessoal", f.Logs.All);
        Assert.DoesNotContain("Abc12345", f.Logs.All); Assert.DoesNotContain("123.456", f.Logs.All);
    }

    [Fact]
    public async Task AuditFailureNeverReleasesBlockedData()
    {
        var audit = new SecurityAudit(new BrokenScopeFactory(), NullLogger<SecurityAudit>.Instance);
        var guard = new SinkGuard(audit, NullLogger<SinkGuard>.Instance);
        Assert.False(await guard.VerifyAsync("x", SinkDestino.RespostaPublica, ("llm.resposta", "CPF 123.456.789-00")));
        await Assert.ThrowsAsync<ApiException>(() => guard.RequireCleanAsync("x", SinkDestino.Persistencia, [("fonte.texto", "maria@example.org")]));
        Assert.DoesNotContain("123.456", await guard.SanitizeAsync("x", "CPF 123.456.789-00", "conversa.pergunta", SinkDestino.Llm));
    }

    // ---------- Persistência / publicação ----------

    [Fact]
    public async Task AdminRejectsPersonalDataInSourcesAndDraftsBeforePersistence()
    {
        await using var f = new SecurityFactory(); await f.SeedAsync();
        using var admin = f.Client("ADMIN", SecurityFactory.CityA);
        var source = await admin.PostAsJsonAsync($"/api/v1/municipios/{SecurityFactory.CityA}/fontes", new { titulo = "Fonte fictícia", url = "https://example.org/x", texto = $"Fale com o CPF {Cpf}", dataConsulta = "2026-01-01" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, source.StatusCode);
        var text = await source.Content.ReadAsStringAsync();
        Assert.Contains("dados pessoais", text); Assert.DoesNotContain("123.456", text);
        Assert.Empty(await f.WithDb(db => db.Fontes.ToListAsync()));

        var ok = await admin.PostAsJsonAsync($"/api/v1/municipios/{SecurityFactory.CityA}/fontes", new { titulo = "Fonte fictícia", url = "https://example.org/x", texto = "Texto institucional sem dados pessoais.", dataConsulta = "2026-01-01" });
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        var sourceId = (await ok.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var dirty = new ServicoRevisado("cata-treco", "Cata Treco", "Orientação fictícia.", null, ["Ligue e informe a senha=Abc12345"], [], null, ["sofá"]);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync($"/api/v1/fontes/{sourceId}/rascunhos", new CriarRascunho(dirty))).StatusCode);
        Assert.Empty(await f.WithDb(db => db.Rascunhos.ToListAsync()));
        var clean = dirty with { Etapas = ["Consulte o canal de demonstração."] };
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync($"/api/v1/fontes/{sourceId}/rascunhos", new CriarRascunho(clean))).StatusCode);

        var events = await f.WithDb(db => db.EventosSeguranca.Where(e => e.Acao == AcaoProtecao.Bloqueado).ToListAsync());
        Assert.Contains(events, e => e.Origem == "fonte.texto" && e.Categoria == PiiCategory.Cpf && e.Codigo == "CONTEUDO_REJEITADO" && e.Destino == SinkDestino.Persistencia);
        Assert.Contains(events, e => e.Origem == "rascunho.etapas[0]" && e.Categoria == PiiCategory.Senha);
        Assert.DoesNotContain("Abc12345", JsonSerializer.Serialize(events));
    }

    [Fact]
    public async Task LoginAndUserCreation_PreservePasswordsThatLookLikePii()
    {
        await using var f = new SecurityFactory(); await f.SeedAsync();
        const string password = "senha=Abc12345 cpf 123.456.789-00";
        using var operador = f.Client("OPERADOR");
        var created = await operador.PostAsJsonAsync("/api/v1/usuarios", new { email = "admin.a@example.test", senha = password, municipioId = SecurityFactory.CityA });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var anonymous = f.Client();
        Assert.Equal(HttpStatusCode.OK, (await anonymous.PostAsJsonAsync("/api/v1/auth/login", new { email = "admin.a@example.test", senha = password })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/v1/auth/login", new { email = "admin.a@example.test", senha = "[DADO REMOVIDO]" })).StatusCode);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FacilMunicipio.Api.Data.AppDbContext>();
        var user = await db.Usuarios.SingleAsync(u => u.Email == "admin.a@example.test");
        Assert.Equal(PasswordVerificationResult.Success, new PasswordHasher<Usuario>().VerifyHashedPassword(user, user.SenhaHash, password));
        Assert.DoesNotContain("Abc12345", user.SenhaHash);
        Assert.Empty(await db.EventosSeguranca.ToListAsync());
        Assert.DoesNotContain("Abc12345", f.Logs.All);
    }

    // ---------- 5. Tradução ----------

    private static EventoSeguranca NewEvent(string city = SecurityFactory.CityA, PiiCategory c = PiiCategory.Cpf, AcaoProtecao a = AcaoProtecao.Sanitizado,
        string origem = "conversa.pergunta", DateTimeOffset? at = null) => new()
    {
        CorrelacaoId = Guid.NewGuid(), MunicipioId = city, Categoria = c, Acao = a, Origem = origem, Destino = SinkDestino.Llm,
        Codigo = a == AcaoProtecao.Sanitizado ? "PII_SANITIZADA" : "SINK_BLOQUEADO", Gravidade = RiskRules.Gravidade(c), OcorridoEm = at ?? DateTimeOffset.UtcNow
    };

    private static OpenAiCompatibleRiskTranslator Translator(Func<HttpRequestMessage, string?, HttpResponseMessage> reply, List<string> bodies, bool configured = true) =>
        new(new HttpClient(new StubHandler(async r => { var body = r.Content is null ? null : await r.Content.ReadAsStringAsync(); bodies.Add(Decoded(body)); return reply(r, body); })),
            new ConfigurationBuilder().AddInMemoryCollection(configured ? new Dictionary<string, string?>
            { ["RiskTranslator:BaseUrl"] = "https://llm.example.test/v1", ["RiskTranslator:Model"] = "modelo-de-teste", ["RiskTranslator:ApiKey"] = "chave-de-teste" } : []).Build());

    // JsonContent escapa acentos (á); o teste examina o prompt já decodificado.
    private static string Decoded(string? body)
    {
        if (string.IsNullOrEmpty(body)) return "";
        var root = JsonDocument.Parse(body).RootElement;
        return "model=" + root.GetProperty("model").GetString() + " | "
            + root.GetProperty("messages")[0].GetProperty("content").GetString();
    }

    private static HttpResponseMessage Completion(string text) => new(HttpStatusCode.OK)
    { Content = JsonContent.Create(new { choices = new[] { new { message = new { content = text } } } }) };

    [Fact]
    public async Task Translation_PromptHasOnlyControlledMetadata_AndSuccessIsStoredAsAi()
    {
        await using var f = new SecurityFactory(); await f.SeedAsync();
        using var client = f.Client();
        await client.PostAsJsonAsync($"/api/v1/municipios/{SecurityFactory.CityA}/conversas", new EnvioPergunta($"Meu CPF é {Cpf}, senha=Abc12345 e tenho diabetes. maria@example.org", null));
        var bodies = new List<string>(); HttpRequestMessage? seen = null;
        var translator = Translator((r, _) => { seen = r; return Completion("Retiramos seu CPF do texto antes de seguir. Seus dados continuam protegidos."); }, bodies);
        await f.WithDb(async db =>
        {
            Assert.Equal(4, await new RiskTranslationProcessor(db, translator, NullLogger<RiskTranslationProcessor>.Instance).ProcessPendingAsync(10, default));
            return 0;
        });
        Assert.NotEmpty(bodies);
        Assert.All(bodies, b =>
        {
            Assert.All(Originals, secret => Assert.DoesNotContain(secret, b, StringComparison.OrdinalIgnoreCase));
            Assert.Contains("no máximo duas frases", b); Assert.Contains("português do Brasil", b); Assert.Contains("modelo-de-teste", b);
        });
        Assert.Equal("Bearer", seen!.Headers.Authorization!.Scheme);
        var events = await f.WithDb(db => db.EventosSeguranca.ToListAsync());
        Assert.All(events, e => { Assert.Equal(TraducaoEstado.Traduzida, e.TraducaoEstado); Assert.NotNull(e.TraducaoTexto); });
        Assert.DoesNotContain(events, e => e.TraducaoTexto!.Contains("123.456"));
    }

    [Fact]
    public async Task Translation_ReusesIdenticalTranslationsInsteadOfCallingTheProviderAgain()
    {
        await using var f = new SecurityFactory(); await f.SeedAsync();
        await f.WithDb(async db => { db.EventosSeguranca.AddRange(NewEvent(), NewEvent(), NewEvent()); await db.SaveChangesAsync(); return 0; });
        var bodies = new List<string>();
        await f.WithDb(async db =>
        {
            await new RiskTranslationProcessor(db, Translator((_, _) => Completion("Removemos um dado pessoal do texto. Nada foi exibido."), bodies), NullLogger<RiskTranslationProcessor>.Instance).ProcessPendingAsync(10, default);
            return 0;
        });
        Assert.Single(bodies);
        Assert.All(await f.WithDb(db => db.EventosSeguranca.ToListAsync()), e => Assert.Equal(TraducaoEstado.Traduzida, e.TraducaoEstado));
    }

    [Fact]
    public async Task Translation_UnconfiguredProviderKeepsEventsPending_AndDashboardShowsLabeledDefaultMessage()
    {
        await using var f = new SecurityFactory(); await f.SeedAsync();
        await f.WithDb(async db => { db.EventosSeguranca.Add(NewEvent()); await db.SaveChangesAsync(); return 0; });
        var bodies = new List<string>();
        await f.WithDb(async db =>
        {
            Assert.Equal(0, await new RiskTranslationProcessor(db, Translator((_, _) => Completion("x"), bodies, configured: false), NullLogger<RiskTranslationProcessor>.Instance).ProcessPendingAsync(10, default));
            return 0;
        });
        Assert.Empty(bodies);
        using var admin = f.Client("ADMIN", SecurityFactory.CityA);
        var panel = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/municipios/{SecurityFactory.CityA}/seguranca/painel");
        var ev = panel.GetProperty("eventos")[0];
        Assert.Equal("PADRAO", ev.GetProperty("origemExplicacao").GetString());
        Assert.Equal("Pendente", ev.GetProperty("traducaoEstado").GetString());
        Assert.Equal(RiskRules.MensagemPadrao(AcaoProtecao.Sanitizado), ev.GetProperty("explicacao").GetString());
        Assert.Equal(1, panel.GetProperty("resumo").GetProperty("traducoesPendentes").GetInt32());
    }

    [Fact]
    public async Task Translation_FailuresAreBoundedAndMarkedFailed_WithoutLosingTheEvent()
    {
        await using var f = new SecurityFactory(); await f.SeedAsync();
        await f.WithDb(async db => { db.EventosSeguranca.Add(NewEvent()); await db.SaveChangesAsync(); return 0; });
        var bodies = new List<string>();
        var down = Translator((_, _) => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable), bodies);
        for (var i = 0; i < 5; i++)
            await f.WithDb(async db => { await new RiskTranslationProcessor(db, down, NullLogger<RiskTranslationProcessor>.Instance).ProcessPendingAsync(10, default); return 0; });
        Assert.Equal(RiskTranslationProcessor.MaxAttempts, bodies.Count);
        var e = (await f.WithDb(db => db.EventosSeguranca.ToListAsync())).Single();
        Assert.Equal(TraducaoEstado.Falhou, e.TraducaoEstado); Assert.Null(e.TraducaoTexto);
        using var admin = f.Client("ADMIN", SecurityFactory.CityA);
        var ev = (await admin.GetFromJsonAsync<JsonElement>($"/api/v1/municipios/{SecurityFactory.CityA}/seguranca/painel")).GetProperty("eventos")[0];
        Assert.Equal("PADRAO", ev.GetProperty("origemExplicacao").GetString());
    }

    [Theory]
    [InlineData("Um ataque hacker tentou invadir sua conta. Fique atento.")]
    [InlineData("Uma frase. Duas frases. Três frases.")]
    [InlineData("Seu CPF 123.456.789-00 foi removido.")]
    [InlineData("")]
    public void Translation_OutputOfTheModelIsAlsoValidated(string output) => Assert.False(RiskTranslationProcessor.IsAcceptable(output));

    [Fact]
    public void Translation_AcceptsShortFaithfulPortuguese() =>
        Assert.True(RiskTranslationProcessor.IsAcceptable("Removemos um dado pessoal do texto antes de continuar. Nada foi exibido a ninguém."));

    [Fact]
    public async Task Retention_RemovesOnlyExpiredEvents()
    {
        await using var f = new SecurityFactory(); await f.SeedAsync();
        await f.WithDb(async db =>
        {
            db.EventosSeguranca.AddRange(NewEvent(at: DateTimeOffset.UtcNow.AddDays(-120)), NewEvent());
            await db.SaveChangesAsync();
            Assert.Equal(1, await new RiskTranslationProcessor(db, Translator((_, _) => Completion("x"), []), NullLogger<RiskTranslationProcessor>.Instance).PurgeExpiredAsync(90, default));
            Assert.Single(await db.EventosSeguranca.ToListAsync());
            return 0;
        });
    }

    // ---------- 6. Dashboard ----------

    [Fact]
    public async Task DashboardApi_RequiresJwtAndEnforcesMunicipalIsolation()
    {
        await using var f = new SecurityFactory(); await f.SeedAsync();
        await f.WithDb(async db => { db.EventosSeguranca.AddRange(NewEvent(), NewEvent(SecurityFactory.CityB)); await db.SaveChangesAsync(); return 0; });
        var a = $"/api/v1/municipios/{SecurityFactory.CityA}/seguranca/painel"; var b = $"/api/v1/municipios/{SecurityFactory.CityB}/seguranca/painel";
        using var anonymous = f.Client(); using var adminA = f.Client("ADMIN", SecurityFactory.CityA); using var operador = f.Client("OPERADOR");
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(a)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await adminA.GetAsync(a)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await adminA.GetAsync(b)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await operador.GetAsync(b)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await operador.GetAsync("/api/v1/municipios/nao-existe/seguranca/painel")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await adminA.GetAsync(a + "?horas=0")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await adminA.GetAsync(a + "?horas=999")).StatusCode);
        var panel = await adminA.GetFromJsonAsync<JsonElement>(a);
        Assert.Equal(1, panel.GetProperty("resumo").GetProperty("total").GetInt32());
        Assert.Equal(1, panel.GetProperty("resumo").GetProperty("sanitizados").GetInt32());
        Assert.Equal("AMARELO", panel.GetProperty("indicador").GetProperty("nivel").GetString());
    }

    [Fact]
    public async Task DashboardApi_RespectsThePeriod()
    {
        await using var f = new SecurityFactory(); await f.SeedAsync();
        await f.WithDb(async db => { db.EventosSeguranca.AddRange(NewEvent(at: DateTimeOffset.UtcNow.AddHours(-30)), NewEvent(a: AcaoProtecao.Bloqueado)); await db.SaveChangesAsync(); return 0; });
        using var admin = f.Client("ADMIN", SecurityFactory.CityA);
        var day = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/municipios/{SecurityFactory.CityA}/seguranca/painel?horas=24");
        var week = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/municipios/{SecurityFactory.CityA}/seguranca/painel?horas=168");
        Assert.Equal(1, day.GetProperty("resumo").GetProperty("total").GetInt32()); Assert.Equal(1, day.GetProperty("resumo").GetProperty("bloqueados").GetInt32());
        Assert.Equal(2, week.GetProperty("resumo").GetProperty("total").GetInt32());
    }

    [Fact]
    public void RiskIndicator_SeparatesPotentialSeverityFromProtectionStatus()
    {
        var none = RiskRules.Indicador([], 24);
        Assert.Equal("VERDE", none.Nivel); Assert.Contains("não é garantia", none.Descricao); Assert.DoesNotContain("seguro", none.Rotulo.ToLowerInvariant());
        Assert.Equal("VERDE", RiskRules.Indicador([(AcaoProtecao.Sanitizado, Gravidade.Baixa)], 24).Nivel);
        Assert.Equal("AMARELO", RiskRules.Indicador([(AcaoProtecao.Sanitizado, Gravidade.Media)], 24).Nivel);
        // Bloqueio bem-sucedido de gravidade alta NÃO vira vermelho nem "vazamento confirmado".
        var blocked = RiskRules.Indicador([(AcaoProtecao.Bloqueado, Gravidade.Alta)], 24);
        Assert.Equal("AMARELO", blocked.Nivel); Assert.DoesNotContain("vazamento confirmado", blocked.Descricao, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("VERMELHO", RiskRules.Indicador([(AcaoProtecao.Pendente, Gravidade.Alta)], 24).Nivel);
        Assert.Equal("AMARELO", RiskRules.Indicador([(AcaoProtecao.Pendente, Gravidade.Baixa)], 24).Nivel);
    }

    [Fact]
    public async Task DashboardPage_IsStaticAndEmbedsNoEventsOrCredentialStorage()
    {
        await using var f = new SecurityFactory(); await f.SeedAsync();
        await f.WithDb(async db => { db.EventosSeguranca.Add(NewEvent()); await db.SaveChangesAsync(); return 0; });
        using var client = f.Client();
        var response = await client.GetAsync("/auditoria/");
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Auditoria de Privacidade — Fácil Município", html); Assert.Contains("Risco Cidadão", html);
        Assert.DoesNotContain("localStorage", html); Assert.DoesNotContain("sessionStorage", html); Assert.DoesNotContain("PII_SANITIZADA", html);
        Assert.DoesNotContain("innerHTML", html); Assert.DoesNotContain("?token=", html);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => handler(request);
    }

    private sealed class BrokenScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => throw new InvalidOperationException("auditoria indisponível");
    }
}
