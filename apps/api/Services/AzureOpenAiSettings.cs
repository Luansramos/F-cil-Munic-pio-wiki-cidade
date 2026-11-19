using System.ClientModel;
using OpenAI;
using OpenAI.Chat;

namespace FacilMunicipio.Api.Services;

/// <summary>Localiza as credenciais Azure e cria um cliente compatível com a API OpenAI v1.</summary>
public sealed class AzureOpenAiSettings
{
    public const string DefaultDeployment = "gpt-5-mini";

    private AzureOpenAiSettings(string endpoint, string apiKey, string deployment)
    {
        Endpoint = endpoint;
        ApiKey = apiKey;
        Deployment = deployment;
    }

    public string Endpoint { get; }
    public string Deployment { get; }
    private string ApiKey { get; }

    public static AzureOpenAiSettings? FromConfiguration(IConfiguration configuration)
    {
        var endpoint = First(configuration["Llm:Azure:Endpoint"], configuration["ENDPOINT_AZURE"]);
        var apiKey = First(configuration["Llm:Azure:ApiKey"], configuration["LLM_KEY_AZURE"]);
        var deployment = First(configuration["Llm:Azure:Deployment"], configuration["LLM_AZURE_DEPLOYMENT"])
            ?? DefaultDeployment;

        if (string.IsNullOrWhiteSpace(endpoint) && string.IsNullOrWhiteSpace(apiKey))
            return null;
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Configure ENDPOINT_AZURE e LLM_KEY_AZURE para habilitar o provedor Azure.");

        var parsedEndpoint = NormalizeEndpoint(endpoint);
        if (string.IsNullOrWhiteSpace(deployment) || deployment.Length > 200)
            throw new InvalidOperationException("LLM_AZURE_DEPLOYMENT deve conter o nome válido do deployment.");

        return new AzureOpenAiSettings(parsedEndpoint, apiKey.Trim(), deployment.Trim());
    }

    public ChatClient CreateChatClient() => new(
        Deployment,
        new ApiKeyCredential(ApiKey),
        new OpenAIClientOptions { Endpoint = new Uri(Endpoint, UriKind.Absolute) });

    private static string? First(string? preferred, string? fallback)
        => string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;

    private static string NormalizeEndpoint(string endpoint)
    {
        if (!Uri.TryCreate(endpoint.Trim(), UriKind.Absolute, out var parsed)
            || parsed.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(parsed.UserInfo)
            || !string.IsNullOrEmpty(parsed.Query)
            || !string.IsNullOrEmpty(parsed.Fragment))
            throw new InvalidOperationException("ENDPOINT_AZURE deve ser uma URL HTTPS do Azure OpenAI v1, sem query string.");

        var path = parsed.AbsolutePath.TrimEnd('/');
        if (string.IsNullOrEmpty(path))
            path = "/openai/v1";
        else if (!path.Equals("/openai/v1", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ENDPOINT_AZURE deve ser o endpoint OpenAI v1, com ou sem o caminho /openai/v1.");

        return new UriBuilder(parsed.Scheme, parsed.Host, parsed.Port, path + "/").Uri.AbsoluteUri;
    }
}
