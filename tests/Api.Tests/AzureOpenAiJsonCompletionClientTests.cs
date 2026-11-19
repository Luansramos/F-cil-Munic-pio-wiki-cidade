using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Text;
using System.Text.Json;
using FacilMunicipio.Api.Services;
using OpenAI;
using OpenAI.Chat;

namespace Api.Tests;

public sealed class AzureOpenAiJsonCompletionClientTests
{
    [Fact]
    public async Task SendsStructuredChatRequestToConfiguredAzureEndpoint()
    {
        const string completionText = "{\"ok\":true}";
        var handler = new RecordingHandler(ChatCompletionResponse(completionText));
        var client = CreateClient(handler);

        var response = await client.CompleteAsync(
            "system instructions", "user input", "test_schema",
            "{\"type\":\"object\",\"properties\":{\"ok\":{\"type\":\"boolean\"}}}",
            123, CancellationToken.None);

        Assert.Equal(completionText, response);
        Assert.NotNull(handler.RequestUri);
        Assert.Equal("https://azure.test/openai/v1/chat/completions", handler.RequestUri.AbsoluteUri);
        Assert.NotNull(handler.RequestBody);
        using var request = JsonDocument.Parse(handler.RequestBody);
        var root = request.RootElement;
        Assert.Equal("gpt-5-mini", root.GetProperty("model").GetString());
        Assert.Equal(123, root.GetProperty("max_completion_tokens").GetInt32());
        var messages = root.GetProperty("messages");
        Assert.Equal(2, messages.GetArrayLength());
        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Equal("system instructions", messages[0].GetProperty("content").GetString());
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
        Assert.Equal("user input", messages[1].GetProperty("content").GetString());
        var format = root.GetProperty("response_format");
        Assert.Equal("json_schema", format.GetProperty("type").GetString());
        var jsonSchema = format.GetProperty("json_schema");
        Assert.Equal("test_schema", jsonSchema.GetProperty("name").GetString());
        Assert.True(jsonSchema.GetProperty("strict").GetBoolean());
        Assert.Equal("object", jsonSchema.GetProperty("schema").GetProperty("type").GetString());
    }

    [Fact]
    public async Task ReturnsNullWhenTheCompletionHasNoTextParts()
    {
        var handler = new RecordingHandler(ChatCompletionResponse(null));
        var client = CreateClient(handler);

        var response = await client.CompleteAsync(
            "system", "user", "schema", "{}", 50, CancellationToken.None);

        Assert.Null(response);
    }

    [Fact]
    public async Task PropagatesCancellationToTheHttpTransport()
    {
        var handler = new RecordingHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var client = CreateClient(handler);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.CompleteAsync(
            "system", "user", "schema", "{}", 50, cancellation.Token));
    }

    [Fact]
    public async Task PropagatesHttpProviderErrorsToTheWikiBoundary()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("provider unavailable")
        });
        var client = CreateClient(handler);

        var error = await Assert.ThrowsAsync<ClientResultException>(() => client.CompleteAsync(
            "system", "user", "schema", "{}", 50, CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, error.Status);
    }

    private static AzureOpenAiJsonCompletionClient CreateClient(RecordingHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri("https://azure.test/openai/v1/"),
            Transport = new HttpClientPipelineTransport(httpClient)
        };
        var chat = new ChatClient("gpt-5-mini", new ApiKeyCredential("unit-test-key"), options);
        return new AzureOpenAiJsonCompletionClient(chat);
    }

    private static HttpResponseMessage ChatCompletionResponse(string? content)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                id = "chatcmpl-unit-test",
                @object = "chat.completion",
                created = 123,
                model = "gpt-5-mini",
                choices = new[]
                {
                    new
                    {
                        index = 0,
                        message = new { role = "assistant", content },
                        finish_reason = "stop"
                    }
                },
                usage = new { prompt_tokens = 1, completion_tokens = 1, total_tokens = 2 }
            }), Encoding.UTF8, "application/json")
        };

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;

        public RecordingHandler(HttpResponseMessage response)
            : this((_, _) => Task.FromResult(response)) { }

        public RecordingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
            => _respond = respond;

        public Uri? RequestUri { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return await _respond(request, cancellationToken);
        }
    }
}
