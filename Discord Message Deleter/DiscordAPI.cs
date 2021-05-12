using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Discord_Delete_Messages
{
    class DiscordAPI
    {
        public class DiscordMessageChannel
        {
            public DiscordMessageChannel()
            {
                isGuild = false;
                channelID = string.Empty;
            }

            public bool isGuild;
            public string channelID;
        }
        public class DiscordSearchResult
        {
            public DiscordSearchResult()
            {
                messageList = new List<QuickType.Message>();
                TotalResults = 0;
            }

            public List<QuickType.Message> messageList;
            public int TotalResults;
        }

        private const string discordApiUrl = "https://discordapp.com/api/v9/";
        private readonly HttpClient httpClient = new HttpClient();
        private string lastUserID = null;

        public async Task SetAuthID(string AuthID, Utils.RateLimitCallbackDelegate rateLimitCallback = null,
            CancellationToken ct = default)
        {
            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Add("Authorization", AuthID);
            lastUserID = (await InternalGetUserID(rateLimitCallback, ct)).Id;
        }

        private async Task<QuickType.JsonGetIDField> InternalGetUserID(Utils.RateLimitCallbackDelegate rateLimitCallback = null,
            CancellationToken ct = default)
        {
            return QuickType.JsonGetIDField.FromJson(await Utils.HttpGetStringAndWaitRatelimit(httpClient, discordApiUrl + "users/@me", rateLimitCallback, ct));
        }

        private async Task<DiscordSearchResult> InternalMakeDiscordSearchRequest(string channelId, string userId, int offset, bool isGuild,
            Utils.RateLimitCallbackDelegate rateLimitCallback = null, CancellationToken ct = default)
        {

            string targetRestUrl = "/" + (isGuild ? "guilds" : "channels");
            targetRestUrl += $"/{channelId}/messages/search?author_id={userId}";
            targetRestUrl += (offset != 0) ? $"&offset={offset}" : "";

            QuickType.SearchRequestResponse result = null;

            while (result == null || result.Messages == null)
            {
                result = QuickType.SearchRequestResponse.FromJson(await Utils.HttpGetStringAndWaitRatelimit(httpClient, discordApiUrl + targetRestUrl, rateLimitCallback, ct));
            }

            DiscordSearchResult search_result = new DiscordSearchResult
            {
                TotalResults = result.TotalResults
            };

            search_result.messageList.AddRange(result.Messages.SelectMany(messageChunk => messageChunk
                .Where(message => message.Author.Id == userId)).DistinctBy(x => x.Id));

            return search_result;
        }

        public async Task<List<QuickType.JsonGetIDField>> GetUsersGuilds(Utils.RateLimitCallbackDelegate rateLimitCallback = null, CancellationToken ct = default)
        {
            return QuickType.JsonGetIDField.FromJsonList(
                await Utils.HttpGetStringAndWaitRatelimit(httpClient, discordApiUrl + "users/@me/guilds", rateLimitCallback, ct));
        }

        public async Task<List<QuickType.DmChatGroup>> GetUsersDmList(Utils.RateLimitCallbackDelegate rateLimitCallback = null, CancellationToken ct = default)
        {
            return QuickType.DmChatGroup.FromJsonList(
                await Utils.HttpGetStringAndWaitRatelimit(httpClient, discordApiUrl + "users/@me/channels", rateLimitCallback, ct));
        }

        public delegate void SearchProgressCallbackDelegate(int foundMessageCount);

        public async Task<DiscordSearchResult> GetAllMessagesByUserInChannel(string channelId, string userId, bool isGuild, bool onlyNormalMessages,
            Utils.RateLimitCallbackDelegate rateLimitCallback = null, SearchProgressCallbackDelegate progressCallback = null, CancellationToken ct = default)
        {
            var combinedSearchResult = new DiscordSearchResult();
            int currentOffset = 0;

            while (currentOffset <= 5000)
            {
                var currentSearchResults = await InternalMakeDiscordSearchRequest(channelId, userId, currentOffset, isGuild, rateLimitCallback, ct);

                int currentMessageCount = currentSearchResults.messageList.Count;
                if (currentMessageCount == 0)
                {
                    break;
                }
                currentOffset += currentMessageCount;
                progressCallback?.Invoke(currentOffset);

                combinedSearchResult.messageList.AddRange(currentSearchResults.messageList);
            }

            if (onlyNormalMessages)
            {
                combinedSearchResult.messageList = combinedSearchResult.messageList.Where(x => x.Type == 0 || x.Type == 19 || x.Type == 20).ToList();
            }

            //We have to be sure that there are no duplicates
            combinedSearchResult.messageList = combinedSearchResult.messageList.DistinctBy(x => x.Id).ToList();
            combinedSearchResult.TotalResults = combinedSearchResult.messageList.Count;

            return combinedSearchResult;
        }

        public async Task DeleteMessagesFromMessageList(List<QuickType.Message> messageList, Action<int> messageDeleteCallback = null,
            Utils.RateLimitCallbackDelegate rateLimitCallback = null, CancellationToken ct = default)
        {
            int deletedCount = 0;

            messageList = messageList.Where(x => x.Author.Id == lastUserID).ToList();
            foreach (var message in messageList)
            {
                try
                {
                    using (var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Delete,
                        RequestUri = new Uri(discordApiUrl + $"channels/{message.ChannelId}/messages/{message.Id}")
                    })
                    {
                        var response = await Utils.HttpRequestAndWaitRatelimit(httpClient, request, rateLimitCallback, ct);
                    }
                    messageDeleteCallback?.Invoke(++deletedCount);
                }
                catch { }
            }
        }

        public delegate void DeleteProgressCallbackDelegate(int totalChannelCount, int searchedChannelCount, int foundMessageCount, int deletedMessageCount);

        public async Task DeleteMessagesFromMultipleChannels(DiscordMessageChannel[] channelList,
            DeleteProgressCallbackDelegate progressCallback = null,
            Utils.RateLimitCallbackDelegate rateLimitCallback = null, CancellationToken ct = default)
        {
            List<Task> taskList = new List<Task>();
            int totalMessageCount = 0, deletedCount = 0;
            foreach (DiscordMessageChannel channel in channelList)
            {
                var messageList = await GetAllMessagesByUserInChannel(channel.channelID, lastUserID, channel.isGuild, true,
                    rateLimitCallback, (int foundMessages) =>
                    {
                        progressCallback?.Invoke(channelList.Length, taskList.Count, totalMessageCount + foundMessages, deletedCount);
                    }, ct);

                totalMessageCount += messageList.TotalResults;

                taskList.Add(Task.Run(() => DeleteMessagesFromMessageList(messageList.messageList, new Action<int>((int _deletedCount) =>
                {
                    deletedCount++;
                    progressCallback?.Invoke(channelList.Length, taskList.Count, totalMessageCount, deletedCount);
                }), rateLimitCallback, ct)));

                progressCallback?.Invoke(channelList.Length, taskList.Count, totalMessageCount, deletedCount);
            }

            await Task.WhenAll(taskList);
        }
    }
}
