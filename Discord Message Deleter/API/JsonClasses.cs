using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Discord_Message_Deleter.API;

public static class JsonExtensions
{
    public static T ParseAs<T>(this string json)
    {
        return JsonSerializer.Deserialize<T>(json) ?? throw new JsonException($"Null result for {typeof(T).Name}");
    }
}

public record User([property: JsonPropertyName("id")] string Id);

public record Guild([property: JsonPropertyName("id")] string Id);

public record ChannelInfo(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("guild_id")] string? GuildId
);

public record APIError(
    [property: JsonPropertyName("code")] long Code,
    [property: JsonPropertyName("message")] string Message
);

public record SearchRequestResponse(
    [property: JsonPropertyName("messages")] List<List<Message>> Messages
);

public record Message(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] long Type,
    [property: JsonPropertyName("channel_id")] string ChannelId
);
