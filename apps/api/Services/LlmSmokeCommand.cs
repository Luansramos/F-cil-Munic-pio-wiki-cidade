using FacilMunicipio.Api.Contracts;
using System.ClientModel;
using System.Text.Json;
using OpenAI.Chat;

namespace FacilMunicipio.Api.Services;

/// <summary>Faz duas chamadas controladas sem acessar os bancos ou imprimir conteúdo.</summary>
public static class LlmSmokeCommand
{
    public static async Task<int> RunAsync(AzureOpenAiSettings settings)
    {
        var stage = "inicialização";
        try
        {
            var wiki = new AzureOpenAiWiki(new AzureOpenAiJsonCompletionClient(settings.CreateChatClient()));
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            const string source = "Serviço fictício para validar a extração estruturada. " +
                "Para solicitar o cartão municipal de leitura, preencha o formulário fictício L-42 e anexe uma foto do comprovante de residência.";

            stage = "extração";
            var draft = await wiki.ExtrairAsync(source, timeout.Token);
            if (string.IsNullOrWhiteSpace(draft.Nome) || string.IsNullOrWhiteSpace(draft.Descricao))
                throw new InvalidOperationException();

            var service = new ServicoRevisado(
                "smoke-leitura",
                "Cartão municipal de leitura fictício",
                "Para solicitar o cartão municipal de leitura, preencha o formulário fictício L-42 e anexe uma foto do comprovante de residência.",
                null,
                ["Preencha o formulário fictício L-42."],
                ["Anexe uma foto do comprovante de residência."],
                null,
                ["cartão de leitura"]);
            var context = new Conhecimento(service, new FonteDto(
                "Fonte sintética de validação", "https://example.test/fonte", null));
            stage = "resposta fundamentada";
            var answer = await wiki.ResponderAsync(
                "Qual comprovante devo anexar para solicitar o cartão de leitura?",
                [],
                [context],
                timeout.Token);

            if (!answer.InformacaoSuficiente
                || answer.ServicoIds.Length != 1
                || answer.ServicoIds[0] != service.Id
                || string.IsNullOrWhiteSpace(answer.Resposta))
                throw new InvalidOperationException();

            stage = "concluído";
            Console.WriteLine($"Azure OpenAI SDK OK; modelo/deployment: {settings.Deployment}; extração estruturada e resposta com evidência aprovadas.");
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Smoke LLM expirou após 60 segundos; confira deployment, endpoint e disponibilidade do modelo.");
            return 1;
        }
        catch (Exception error)
        {
            var cause = error;
            while (cause.InnerException is not null)
                cause = cause.InnerException;
            var status = cause is ClientResultException clientError && clientError.Status > 0
                ? $"; HTTP {clientError.Status}"
                : "";
            var serviceCode = cause is ClientResultException resultError
                ? SafeServiceErrorCode(resultError)
                : null;
            var code = serviceCode is null ? "" : $"; código Azure: {serviceCode}";
            Console.Error.WriteLine($"Smoke LLM falhou na etapa {stage}; causa técnica: {cause.GetType().Name}{status}{code}. Nenhum detalhe secreto foi exibido.");
            return 1;
        }
    }

    private static string? SafeServiceErrorCode(ClientResultException error)
    {
        try
        {
            var response = error.GetRawResponse();
            if (response is null)
                return null;
            using var document = JsonDocument.Parse(response.Content.ToString());
            var root = document.RootElement;
            var serviceError = root.TryGetProperty("error", out var nested) && nested.ValueKind == JsonValueKind.Object
                ? nested
                : root;
            if (serviceError.TryGetProperty("code", out var code)
                && code.ValueKind == JsonValueKind.String)
            {
                var value = code.GetString();
                return value is not null && System.Text.RegularExpressions.Regex.IsMatch(value, "^[A-Za-z0-9_.-]{1,80}$")
                    ? value
                    : null;
            }
        }
        catch (Exception)
        {
            // Provider bodies and messages are deliberately not printed.
        }

        return null;
    }
}
