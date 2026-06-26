using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Discord_Message_Deleter.API;

public static class JsonExtensions
{
    public static T ParseAs<T>(this string json)
    {
        // TODO: This should be handled in API class instead
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind == JsonValueKind.Object &&
            doc.RootElement.TryGetProperty("code", out _) &&
            doc.RootElement.TryGetProperty("message", out _))
        {
            var e = JsonSerializer.Deserialize<APIError>(json)!;
            throw new Exception($"Discord API Error {e.Code}: {e.Message}");
        }
        return JsonSerializer.Deserialize<T>(json) ?? throw new JsonException($"Null result for {typeof(T).Name}");
    }
}

public record JsonGetIDField([property: JsonPropertyName("id")] string Id);

public record APIError(
    [property: JsonPropertyName("code")] long Code,
    [property: JsonPropertyName("message")] string Message
);

public record DmChatGroup(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] long Type,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("last_message_id")] string? LastMessageId,
    [property: JsonPropertyName("owner_id")] string? OwnerId,
    [property: JsonPropertyName("icon")] string? Icon,
    [property: JsonPropertyName("recipients")] List<JsonGetIDField>? Recipients
);

public record SearchRequestResponse(
    [property: JsonPropertyName("total_results")] int TotalResults,
    [property: JsonPropertyName("messages")] List<List<Message>> Messages
);

public record Message(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] long Type,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("pinned")] bool Pinned,
    [property: JsonPropertyName("author")] Author Author,
    [property: JsonPropertyName("channel_id")] string ChannelId
);

public record Author([property: JsonPropertyName("id")] string Id);
