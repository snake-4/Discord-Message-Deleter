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
    public record SearchLocation(string ID, bool IsGuildID);

    /*
     * This list includes all message types marked as 'true' or 'true*' in the Discord API documentation.
     * Note: Type 24 (AUTO_MODERATION_ACTION) can only be deleted by members with 'MANAGE_MESSAGES'.
     */
    public static readonly HashSet<long> DeletableMessageTypes =
    [
        0,  // DEFAULT
        6,  // CHANNEL_PINNED_MESSAGE
        7,  // USER_JOIN
        8,  // GUILD_BOOST
        9,  // GUILD_BOOST_TIER_1
        10, // GUILD_BOOST_TIER_2
        11, // GUILD_BOOST_TIER_3
        12, // CHANNEL_FOLLOW_ADD
        14, // GUILD_DISCOVERY_DISQUALIFIED
        15, // GUILD_DISCOVERY_REQUALIFIED
        16, // GUILD_DISCOVERY_GRACE_PERIOD_INITIAL_WARNING
        17, // GUILD_DISCOVERY_GRACE_PERIOD_FINAL_WARNING
        18, // THREAD_CREATED
        19, // REPLY
        20, // CHAT_INPUT_COMMAND
        22, // GUILD_INVITE_REMINDER
        23, // CONTEXT_MENU_COMMAND
        24, // AUTO_MODERATION_ACTION (Requires MANAGE_MESSAGES)
        25, // ROLE_SUBSCRIPTION_PURCHASE
        26, // INTERACTION_PREMIUM_UPSELL
        27, // STAGE_START
        28, // STAGE_END
        29, // STAGE_SPEAKER
        31, // STAGE_TOPIC
        32, // GUILD_APPLICATION_PREMIUM_SUBSCRIPTION
        36, // GUILD_INCIDENT_ALERT_MODE_ENABLED
        37, // GUILD_INCIDENT_ALERT_MODE_DISABLED
        38, // GUILD_INCIDENT_REPORT_RAID
        39, // GUILD_INCIDENT_REPORT_FALSE_ALARM
        44, // PURCHASE_NOTIFICATION
        46  // POLL_RESULT
    ];

    private const string discordApiUrl = "https://discordapp.com/api/v9/";
    private readonly HttpClient httpClient = new();

    private async Task<string> GetCurrentUserID(Utils.RateLimitCallbackDelegate? rateLimitCallback = null, CancellationToken ct = default)
    {
        return (await Utils.HttpGetString(httpClient, discordApiUrl + "users/@me", rateLimitCallback, ct)).ParseAs<JsonGetIDField>().Id;
    }

    private async Task SetAuthToken(string token)
    {
        httpClient.DefaultRequestHeaders.Remove("Authorization");
        httpClient.DefaultRequestHeaders.Add("Authorization", token);
    }

    private async Task<List<JsonGetIDField>> GetUserGuilds(CancellationToken ct = default)
    {
        return (await Utils.HttpGetString(httpClient, discordApiUrl + "users/@me/guilds", ct: ct)).ParseAs<List<JsonGetIDField>>();
    }

    private async Task<List<DmChatGroup>> GetUserDMList(CancellationToken ct = default)
    {
        return (await Utils.HttpGetString(httpClient, discordApiUrl + "users/@me/channels", ct: ct)).ParseAs<List<DmChatGroup>>();
    }

    private async Task<HttpResponseMessage> UnarchiveThreadChannel(string channelId, Utils.RateLimitCallbackDelegate? rateLimitCallback = null, CancellationToken ct = default)
    {
        return await Utils.HttpRequest(httpClient, () => new HttpRequestMessage(HttpMethod.Patch, discordApiUrl + $"channels/{channelId}")
        {
            Content = new StringContent("{\"archived\":false}", System.Text.Encoding.UTF8, "application/json")
        }, rateLimitCallback, ct);
    }

    private async Task<List<Message>> GetAllMessagesByUserInChannel(SearchLocation channel, string userId, bool filterDeletableMessages,
        Utils.RateLimitCallbackDelegate? rateLimitCallback = null, CancellationToken ct = default)
    {
        int offset = 0;
        var ret = new List<Message>();

        // TODO: Can we instead search messages as they are deleted instead of fetching all messages first?
        while (offset <= 5000)
        {
            // TODO: The endpoint is different for guild channels
            string requestUri = $"{discordApiUrl}{(channel.IsGuildID ? "guilds" : "channels")}/{channel.ID}/messages/search?author_id={userId}{(offset != 0 ? $"&offset={offset}" : "")}";
            var searchResultChunk = (await Utils.HttpGetString(httpClient, requestUri, rateLimitCallback, ct)).ParseAs<SearchRequestResponse>();

            if (searchResultChunk.Messages.Count == 0)
                break;

            offset += searchResultChunk.Messages.Count;
            ret.AddRange(searchResultChunk.Messages.SelectMany(x => x));
        }

        if (filterDeletableMessages)
            return ret.Where(x => DeletableMessageTypes.Contains(x.Type)).DistinctBy(x => x.Id).ToList();
        else
            return ret.DistinctBy(x => x.Id).ToList();
    }

    private async Task DeleteMessagesFromMessageList(List<Message> messageList, IProgress<int>? progress = null,
        Utils.RateLimitCallbackDelegate? rateLimitCallback = null, CancellationToken ct = default)
    {
        int deletedCount = 0;
        foreach (var message in messageList)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var response = await Utils.HttpRequest(httpClient, () => new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri(discordApiUrl + $"channels/{message.ChannelId}/messages/{message.Id}")
                }, rateLimitCallback, ct);
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var error = JsonSerializer.Deserialize<APIError>(json);
                    if (error?.Code == 50083 && !string.IsNullOrEmpty(message.ChannelId)) // Tried to perform an operation on an archived thread
                    {
                        await UnarchiveThreadChannel(message.ChannelId, rateLimitCallback, ct);
                    }
                    await Utils.HttpRequest(httpClient, () => new HttpRequestMessage
                    {
                        Method = HttpMethod.Delete,
                        RequestUri = new Uri(discordApiUrl + $"channels/{message.ChannelId}/messages/{message.Id}")
                    }, rateLimitCallback, ct);
                }
                progress?.Report(++deletedCount);
            }
            catch { }
        }
    }

    public async Task ExecuteDeleteOperationAsync(string authID, string userProvidedIDs, bool eraseAllDMs, bool eraseAllGuilds, IProgress<DeleteProgress> progress, CancellationToken ct = default)
    {
        var progressState = new DeleteProgress();
        void rateLimitCallback(TimeSpan rateLimitTime)
        {
            progressState.StatusMessage = $"Ratelimited for {rateLimitTime.TotalSeconds:n0}s!";
            progress.Report(progressState);
        }

        progressState.StatusMessage = "Fetching information...";
        progress.Report(progressState);

        await SetAuthToken(authID);
        var lastUserID = await GetCurrentUserID(rateLimitCallback, ct);

        progressState.StatusMessage = "Resolving IDs...";
        progress.Report(progressState);

        var finalIDList = await ResolveIDsAsync(userProvidedIDs, eraseAllDMs, eraseAllGuilds, ct);
        if (finalIDList.Count == 0)
        {
            progressState.StatusMessage = "No valid IDs found to search.";
            progress.Report(progressState);
            return;
        }

        progressState.TotalChannelCount = finalIDList.Count;
        progressState.StatusMessage = "Searching messages...";
        progress.Report(progressState);

        var taskList = new List<Task>();

        int localFoundMessageCount = 0;
        int localSearchedChannelCount = 0;
        int localDeletedMessageCount = 0;

        void reportProgress()
        {
            progressState.FoundMessageCount = localFoundMessageCount;
            progressState.SearchedChannelCount = localSearchedChannelCount;
            progressState.DeletedMessageCount = localDeletedMessageCount;
            progress.Report(progressState);
        }

        foreach (var channel in finalIDList)
        {
            var messageList = await GetAllMessagesByUserInChannel(channel, lastUserID, true, rateLimitCallback, ct);

            Interlocked.Add(ref localFoundMessageCount, messageList.Count);
            Interlocked.Increment(ref localSearchedChannelCount);
            reportProgress();

            taskList.Add(Task.Run(() => DeleteMessagesFromMessageList(messageList,
                new Progress<int>(_ =>
                {
                    Interlocked.Increment(ref localDeletedMessageCount);
                    progressState.StatusMessage = "Deleting...";
                    reportProgress();
                }), rateLimitCallback, ct), ct));
        }
        await Task.WhenAll(taskList);

        progressState.StatusMessage = "Finished!";
        progress.Report(progressState);
    }

    private async Task<List<SearchLocation>> ResolveIDsAsync(string userProvidedIDs, bool eraseAllDMs, bool eraseAllGuilds, CancellationToken ct)
    {
        var finalIDList = new List<SearchLocation>();
        var allGuildsList = (await GetUserGuilds(ct)).Select(x => x.Id);

        if (eraseAllGuilds)
            finalIDList.AddRange(allGuildsList.Select(x => new SearchLocation(x, true)));

        if (eraseAllDMs)
        {
            var allDMsList = (await GetUserDMList(ct)).Select(x => x.Id);
            finalIDList.AddRange(allDMsList.Select(x => new SearchLocation(x, false)));
        }

        foreach (string id in userProvidedIDs
            .Split([',', ' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => string.Concat(x.Where(char.IsDigit))))
        {
            if (allGuildsList.Contains(id))
            {
                finalIDList.Add(new SearchLocation(id, true));
            }
            else
            {
                finalIDList.Add(new SearchLocation(id, false));
            }
        }

        return finalIDList.DistinctBy(x => x.ID).ToList();
    }
}
