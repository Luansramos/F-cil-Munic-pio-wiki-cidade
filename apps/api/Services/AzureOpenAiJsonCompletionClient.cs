using OpenAI.Chat;

namespace FacilMunicipio.Api.Services;

/// <summary>Isola a chamada ao SDK para que as regras da wiki possam ser testadas sem rede.</summary>
public interface IAzureOpenAiJsonCompletionClient
{
    Task<string?> CompleteAsync(
        string systemPrompt,
        string userPayload,
        string schemaName,
        string schema,
        int maxOutputTokens,
        CancellationToken ct);
}

public sealed class AzureOpenAiJsonCompletionClient(ChatClient chat) : IAzureOpenAiJsonCompletionClient
{
    public async Task<string?> CompleteAsync(
        string systemPrompt,
        string userPayload,
        string schemaName,
        string schema,
        int maxOutputTokens,
        CancellationToken ct)
    {
        var options = new ChatCompletionOptions
        {
            MaxOutputTokenCount = maxOutputTokens,
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: schemaName,
                jsonSchema: BinaryData.FromString(schema),
                jsonSchemaIsStrict: true)
        };
        var messages = new ChatMessage[]
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPayload)
        };
        var result = await chat.CompleteChatAsync(messages, options, ct);
        return result.Value.Content.FirstOrDefault(part => !string.IsNullOrWhiteSpace(part.Text))?.Text;
    }
}
