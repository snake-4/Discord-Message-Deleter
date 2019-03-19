namespace Discord_Delete_Messages
{
    namespace QuickType
    {
        using System;
        using System.Collections.Generic;

        using System.Globalization;
        using Newtonsoft.Json;
        using Newtonsoft.Json.Converters;

        public partial class OnlyIDExtract
        {
            [JsonProperty("id")]
            public string Id { get; set; }
        }

        public partial class OnlyIDExtract
        {
            public static List<OnlyIDExtract> FromJsonList(string json) => JsonConvert.DeserializeObject<List<OnlyIDExtract>>(json, QuickType.Converter.Settings);
            public static OnlyIDExtract FromJson(string json) => JsonConvert.DeserializeObject<OnlyIDExtract>(json, QuickType.Converter.Settings);

        }

        public partial class DmChatGroup
        {
            [JsonProperty("last_message_id")]
            public string LastMessageId { get; set; }

            [JsonProperty("type")]
            public long Type { get; set; }

            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("owner_id", NullValueHandling = NullValueHandling.Ignore)]
            public string OwnerId { get; set; }

            [JsonProperty("icon")]
            public string Icon { get; set; }
        }

        public partial class DmChatGroup
        {
            public static List<DmChatGroup> FromJsonList(string json) => JsonConvert.DeserializeObject<List<DmChatGroup>>(json, QuickType.Converter.Settings);
            public static List<DmChatGroup> FromJson(string json) => JsonConvert.DeserializeObject<List<DmChatGroup>>(json, QuickType.Converter.Settings);
        }

        public partial class SearchResult
        {
            [JsonProperty("total_results")]
            public long TotalResults { get; set; }

            [JsonProperty("messages")]
            public List<List<Message>> Messages { get; set; }
        }

        public partial class Message
        {
            [JsonProperty("timestamp")]
            public DateTimeOffset Timestamp { get; set; }

            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("pinned")]
            public bool Pinned { get; set; }

            [JsonProperty("author")]
            public Author Author { get; set; }

            [JsonProperty("channel_id")]
            public string ChannelId { get; set; }

            [JsonProperty("type")]
            public long Type { get; set; }

            [JsonProperty("content")]
            public string Content { get; set; }
        }

        public partial class Author
        {
            [JsonProperty("id")]
            public string Id { get; set; }
        }

        public partial class SearchResult
        {
            public static SearchResult FromJson(string json) => JsonConvert.DeserializeObject<SearchResult>(json, QuickType.Converter.Settings);
        }

        internal static class Converter
        {
            public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                DateParseHandling = DateParseHandling.None,
                Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
            };
        }
    }


}
