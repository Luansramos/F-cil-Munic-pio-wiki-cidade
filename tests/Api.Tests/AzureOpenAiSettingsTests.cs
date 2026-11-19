using FacilMunicipio.Api.Services;
using Microsoft.Extensions.Configuration;

namespace Api.Tests;

public class AzureOpenAiSettingsTests
{
    private const string ValidEndpoint = "https://city-wiki.services.ai.azure.com/openai/v1";

    [Fact]
    public void ReturnsNullWhenNoAzureCredentialsAreConfigured()
    {
        var settings = AzureOpenAiSettings.FromConfiguration(Configuration());

        Assert.Null(settings);
    }

    [Theory]
    [InlineData("ENDPOINT_AZURE", ValidEndpoint, "LLM_KEY_AZURE", null)]
    [InlineData("ENDPOINT_AZURE", null, "LLM_KEY_AZURE", "test-key")]
    public void RejectsPartialAzureConfiguration(string firstName, string? firstValue, string secondName, string? secondValue)
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            [firstName] = firstValue,
            [secondName] = secondValue
        });

        var error = Assert.Throws<InvalidOperationException>(
            () => AzureOpenAiSettings.FromConfiguration(configuration));

        Assert.Contains("ENDPOINT_AZURE", error.Message);
        Assert.Contains("LLM_KEY_AZURE", error.Message);
    }

    [Theory]
    [InlineData("https://city-wiki.services.ai.azure.com", "https://city-wiki.services.ai.azure.com/openai/v1/")]
    [InlineData("https://city-wiki.services.ai.azure.com/openai/v1", "https://city-wiki.services.ai.azure.com/openai/v1/")]
    [InlineData("https://city-wiki.services.ai.azure.com/openai/v1/", "https://city-wiki.services.ai.azure.com/openai/v1/")]
    public void NormalizesResourceAndOpenAiV1Endpoints(string endpoint, string expected)
    {
        var settings = AzureOpenAiSettings.FromConfiguration(Configuration(BasicValues(endpoint)));

        Assert.NotNull(settings);
        Assert.Equal(expected, settings.Endpoint);
        Assert.Equal(AzureOpenAiSettings.DefaultDeployment, settings.Deployment);
    }

    [Fact]
    public void UsesNestedSettingsBeforeEnvironmentAliasesAndTrimsValues()
    {
        var settings = AzureOpenAiSettings.FromConfiguration(Configuration(new Dictionary<string, string?>
        {
            ["Llm:Azure:Endpoint"] = "  https://preferred.services.ai.azure.com/openai/v1/ ",
            ["ENDPOINT_AZURE"] = "https://fallback.services.ai.azure.com/openai/v1",
            ["Llm:Azure:ApiKey"] = " preferred-key ",
            ["LLM_KEY_AZURE"] = "fallback-key",
            ["Llm:Azure:Deployment"] = " preferred-deployment ",
            ["LLM_AZURE_DEPLOYMENT"] = "fallback-deployment"
        }));

        Assert.NotNull(settings);
        Assert.Equal("https://preferred.services.ai.azure.com/openai/v1/", settings.Endpoint);
        Assert.Equal("preferred-deployment", settings.Deployment);
        Assert.NotNull(settings.CreateChatClient());
    }

    [Fact]
    public void UsesEnvironmentAliasesAndCustomDeploymentWhenNestedSettingsAreAbsent()
    {
        var settings = AzureOpenAiSettings.FromConfiguration(Configuration(new Dictionary<string, string?>
        {
            ["ENDPOINT_AZURE"] = ValidEndpoint,
            ["LLM_KEY_AZURE"] = "test-key",
            ["LLM_AZURE_DEPLOYMENT"] = " deployment-2026 "
        }));

        Assert.NotNull(settings);
        Assert.Equal("deployment-2026", settings.Deployment);
    }

    [Theory]
    [InlineData("http://city-wiki.services.ai.azure.com")]
    [InlineData("relative-endpoint")]
    [InlineData("https://user:password@city-wiki.services.ai.azure.com/openai/v1")]
    [InlineData("https://city-wiki.services.ai.azure.com/openai/v1?api-version=2026-01-01")]
    [InlineData("https://city-wiki.services.ai.azure.com/openai/v1#fragment")]
    [InlineData("https://city-wiki.services.ai.azure.com/api/projects/city-wiki")]
    public void RejectsMalformedOrUnsupportedEndpoint(string endpoint)
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => AzureOpenAiSettings.FromConfiguration(Configuration(BasicValues(endpoint))));

        Assert.Contains("ENDPOINT_AZURE", error.Message);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("")]
    public void RejectsBlankDeploymentWhenExplicitlyConfigured(string deployment)
    {
        var values = BasicValues(ValidEndpoint);
        values["LLM_AZURE_DEPLOYMENT"] = deployment;

        var error = Assert.Throws<InvalidOperationException>(
            () => AzureOpenAiSettings.FromConfiguration(Configuration(values)));

        Assert.Contains("LLM_AZURE_DEPLOYMENT", error.Message);
    }

    [Fact]
    public void RejectsDeploymentNameLongerThanTwoHundredCharacters()
    {
        var values = BasicValues(ValidEndpoint);
        values["LLM_AZURE_DEPLOYMENT"] = new string('d', 201);

        Assert.Throws<InvalidOperationException>(
            () => AzureOpenAiSettings.FromConfiguration(Configuration(values)));
    }

    [Fact]
    public void AcceptsDeploymentNameAtTheMaximumLength()
    {
        var values = BasicValues(ValidEndpoint);
        var deployment = new string('d', 200);
        values["LLM_AZURE_DEPLOYMENT"] = deployment;

        var settings = AzureOpenAiSettings.FromConfiguration(Configuration(values));

        Assert.Equal(deployment, settings!.Deployment);
    }

    private static Dictionary<string, string?> BasicValues(string endpoint) => new()
    {
        ["ENDPOINT_AZURE"] = endpoint,
        ["LLM_KEY_AZURE"] = "test-key"
    };

    private static IConfiguration Configuration(Dictionary<string, string?>? values = null)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? new Dictionary<string, string?>())
            .Build();
}
