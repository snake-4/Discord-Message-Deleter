using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Discord_Message_Deleter.API;

public class DeleteProgress
{
    public int TotalChannelCount { get; set; }
    public int SearchedChannelCount { get; set; }
    public int FoundMessageCount { get; set; }
    public int DeletedMessageCount { get; set; }
    public string? StatusMessage { get; set; }
}

class DiscordAPI
{
    public record SearchParameters(string AuthorID, string? GuildID = null, string? ChannelID = null);

    public static readonly HashSet<long> DeletableMessageTypes =
    [
        0, 6, 7, 8, 9, 10, 11, 12, 14, 15, 16, 17, 18, 19, 20, 22, 23, 24, 25, 26, 27, 28, 29, 31, 32, 36, 37, 38, 39, 44, 46
    ];

    private const string ApiUrl = "https://discordapp.com/api/v9/";
    private readonly HttpClient http = new();

    public async Task ExecuteDeleteOperationAsync(string authID, string userProvidedIDs, bool eraseAllDMs, bool eraseAllGuilds, IProgress<DeleteProgress> progress, CancellationToken ct = default)
    {
        var state = new DeleteProgress();
        int localFound = 0, localSearched = 0, localDeleted = 0;

        void UpdateUI(string? msg = null)
        {
            if (msg != null) state.StatusMessage = msg;
            state.FoundMessageCount = localFound;
            state.SearchedChannelCount = localSearched;
            state.DeletedMessageCount = localDeleted;
            progress.Report(state);
        }

        void RateLimitCb(TimeSpan t) => UpdateUI($"Ratelimited for {t.TotalSeconds:n0}s!");

        UpdateUI("Fetching information...");
        http.DefaultRequestHeaders.Remove("Authorization");
        http.DefaultRequestHeaders.Add("Authorization", authID);

        var myIdStr = await Utils.HttpGetString(http, ApiUrl + "users/@me", RateLimitCb, ct);
        var myId = ParseResponse<User>(myIdStr).Id;

        UpdateUI("Resolving IDs...");
        // Passed RateLimitCb here since it now makes API calls in a loop
        var targets = await ResolveTargetsAsync(myId, userProvidedIDs, eraseAllDMs, eraseAllGuilds, RateLimitCb, ct);

        if (targets.Count == 0)
        {
            UpdateUI("No valid IDs found to search.");
            return;
        }

        state.TotalChannelCount = targets.Count;
        UpdateUI("Searching messages...");

        var taskList = targets.Select(async target =>
        {
            var deletableMsgs = await FetchDeletableMessagesAsync(target, RateLimitCb, ct);

            Interlocked.Add(ref localFound, deletableMsgs.Count);
            Interlocked.Increment(ref localSearched);
            UpdateUI();

            await DeleteMessagesAsync(deletableMsgs, RateLimitCb, onMessageDeleted: () =>
            {
                Interlocked.Increment(ref localDeleted);
                UpdateUI("Deleting...");
            }, ct);
        });

        await Task.WhenAll(taskList);
        UpdateUI("Finished!");
    }

    private async Task<HashSet<SearchParameters>> ResolveTargetsAsync(string authorId, string userProvidedIDs, bool eraseAllDMs, bool eraseAllGuilds, Utils.RateLimitCallbackDelegate rateLimitCb, CancellationToken ct)
    {
        var targets = new HashSet<SearchParameters>();

        // Fetch user's known guilds and DMs
        var guildsTask = Utils.HttpGetString(http, ApiUrl + "users/@me/guilds", rateLimitCb, ct);
        var dmsTask = Utils.HttpGetString(http, ApiUrl + "users/@me/channels", rateLimitCb, ct);
        await Task.WhenAll(guildsTask, dmsTask);
        var knownGuilds = ParseResponse<List<Guild>>(guildsTask.Result).Select(x => x.Id).ToHashSet();
        var knownDms = ParseResponse<List<ChannelInfo>>(dmsTask.Result).Select(x => x.Id).ToHashSet();

        // Add all guilds and DMs if requested
        if (eraseAllGuilds) targets.UnionWith(knownGuilds.Select(g => new SearchParameters(authorId, GuildID: g)));
        if (eraseAllDMs) targets.UnionWith(knownDms.Select(dm => new SearchParameters(authorId, ChannelID: dm)));

        // Parse user-provided IDs
        var parsedUserIDs = userProvidedIDs
            .Split([',', ' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => new string(x.Where(char.IsDigit).ToArray()))
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct();

        // Resolve specific IDs
        foreach (var id in parsedUserIDs)
        {
            if (knownGuilds.Contains(id))
                targets.Add(new SearchParameters(authorId, GuildID: id));
            else if (knownDms.Contains(id))
                targets.Add(new SearchParameters(authorId, ChannelID: id));
            else
            {
                try
                {
                    // Treat the ID as a channel and confirm if it exists and has a parent guild
                    var channelStr = await Utils.HttpGetString(http, ApiUrl + $"channels/{id}", rateLimitCb, ct);
                    var info = ParseResponse<ChannelInfo>(channelStr);
                    targets.Add(new SearchParameters(authorId, GuildID: info.GuildId, ChannelID: id));
                }
                catch { }
            }
        }

        return targets;
    }

    private async Task<List<Message>> FetchDeletableMessagesAsync(SearchParameters target, Utils.RateLimitCallbackDelegate rateLimitCb, CancellationToken ct)
    {
        // Determine the endpoint based on whether it's a guild or a channel search
        string? endpoint = target.GuildID != null ? $"guilds/{target.GuildID}/messages/search" :
                          target.ChannelID != null ? $"channels/{target.ChannelID}/messages/search" : null;
        if (endpoint == null) return [];

        // Always search for messages by author but specify channel_id if both guild and channel are specified
        string query = $"author_id={target.AuthorID}" + (target.GuildID != null && target.ChannelID != null ? $"&channel_id={target.ChannelID}" : "");
        int offset = 0;
        var msgs = new List<Message>();

        while (offset <= 5000)
        {
            string uri = $"{ApiUrl}{endpoint}?{query}{(offset > 0 ? $"&offset={offset}" : "")}";
            var chunk = ParseResponse<SearchRequestResponse>(await Utils.HttpGetString(http, uri, rateLimitCb, ct)).Messages;

            if (chunk.Count == 0) break;

            offset += chunk.Count;
            msgs.AddRange(chunk.SelectMany(x => x));
        }

        return msgs.Where(x => DeletableMessageTypes.Contains(x.Type)).DistinctBy(x => x.Id).ToList();
    }

    private async Task DeleteMessagesAsync(List<Message> messages, Utils.RateLimitCallbackDelegate rateLimitCb, Action onMessageDeleted, CancellationToken ct)
    {
        foreach (var msg in messages)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                HttpRequestMessage MakeReq() => new(HttpMethod.Delete, ApiUrl + $"channels/{msg.ChannelId}/messages/{msg.Id}");
                using var response = await Utils.HttpRequest(http, MakeReq, rateLimitCb, ct);

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var error = JsonSerializer.Deserialize<APIError>(await response.Content.ReadAsStringAsync(ct));

                    if (error?.Code == 50083 && !string.IsNullOrEmpty(msg.ChannelId))
                    {
                        // Try to unarchive the channel if it's archived
                        await Utils.HttpRequest(http, () => new HttpRequestMessage(HttpMethod.Patch, ApiUrl + $"channels/{msg.ChannelId}")
                        {
                            Content = new StringContent("{\"archived\":false}", System.Text.Encoding.UTF8, "application/json")
                        }, rateLimitCb, ct);
                    }

                    // Retry after unarchiving
                    await Utils.HttpRequest(http, MakeReq, rateLimitCb, ct);
                }

                onMessageDeleted();
            }
            catch (OperationCanceledException) { throw; }
            catch { }
        }
    }

    private static T ParseResponse<T>(string json)
    {
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
