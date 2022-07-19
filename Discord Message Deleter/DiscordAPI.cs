using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordMessageDeleter
{
    class DiscordAPI
    {
        public class ChannelAndGuild
        {
            public ChannelAndGuild(string ID, bool isGuildID)
            {
                this.IsGuildID = isGuildID;
                this.ID = ID;
            }

            public bool IsGuildID;
            public string ID;
        }
        public class DiscordSearchResult
        {
            public DiscordSearchResult()
            {
                MessageList = new List<QuickType.Message>();
                ResultCount = 0;
            }

            public List<QuickType.Message> MessageList;
            public int ResultCount;
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

        private async Task<DiscordSearchResult> InternalMakeDiscordSearchRequest(ChannelAndGuild channel, string userId, int offset,
            Utils.RateLimitCallbackDelegate rateLimitCallback = null, CancellationToken ct = default)
        {

            string targetRestUrl = "/" + (channel.IsGuildID ? "guilds" : "channels");
            targetRestUrl += $"/{channel.ID}/messages/search?author_id={userId}";
            targetRestUrl += (offset != 0) ? $"&offset={offset}" : "";

            QuickType.SearchRequestResponse result = null;

            while (result == null || result.Messages == null)
            {
                result = QuickType.SearchRequestResponse.FromJson(await Utils.HttpGetStringAndWaitRatelimit(httpClient, discordApiUrl + targetRestUrl, rateLimitCallback, ct));
            }

            DiscordSearchResult search_result = new DiscordSearchResult
            {
                ResultCount = result.TotalResults
            };

            search_result.MessageList.AddRange(result.Messages.SelectMany(messageChunk => messageChunk
                .Where(message => message.Author.Id == userId)).DistinctBy(x => x.Id));

            return search_result;
        }

        public async Task<List<QuickType.JsonGetIDField>> GetUserGuilds(Utils.RateLimitCallbackDelegate rateLimitCallback = null, CancellationToken ct = default)
        {
            return QuickType.JsonGetIDField.FromJsonList(
                await Utils.HttpGetStringAndWaitRatelimit(httpClient, discordApiUrl + "users/@me/guilds", rateLimitCallback, ct));
        }

        public async Task<List<QuickType.DmChatGroup>> GetUserDMList(Utils.RateLimitCallbackDelegate rateLimitCallback = null, CancellationToken ct = default)
        {
            return QuickType.DmChatGroup.FromJsonList(
                await Utils.HttpGetStringAndWaitRatelimit(httpClient, discordApiUrl + "users/@me/channels", rateLimitCallback, ct));
        }

        public delegate void SearchProgressCallbackDelegate(int foundMessageCount);

        public async Task<DiscordSearchResult> GetAllMessagesByUserInChannel(ChannelAndGuild channel, string userId, bool onlyNormalMessages,
            Utils.RateLimitCallbackDelegate rateLimitCallback = null, SearchProgressCallbackDelegate progressCallback = null, CancellationToken ct = default)
        {
            var combinedSearchResult = new DiscordSearchResult();
            int currentOffset = 0;

            while (currentOffset <= 5000)
            {
                var currentSearchResults = await InternalMakeDiscordSearchRequest(channel, userId, currentOffset, rateLimitCallback, ct);

                int currentMessageCount = currentSearchResults.MessageList.Count;
                if (currentMessageCount == 0)
                {
                    break;
                }
                currentOffset += currentMessageCount;
                progressCallback?.Invoke(currentOffset);

                combinedSearchResult.MessageList.AddRange(currentSearchResults.MessageList);
            }

            if (onlyNormalMessages)
            {
                combinedSearchResult.MessageList = combinedSearchResult.MessageList.Where(x => x.Type == 0 || x.Type == 19 || x.Type == 20).ToList();
            }

            //We have to be sure that there are no duplicates
            combinedSearchResult.MessageList = combinedSearchResult.MessageList.DistinctBy(x => x.Id).ToList();
            combinedSearchResult.ResultCount = combinedSearchResult.MessageList.Count;

            return combinedSearchResult;
        }

        public async Task DeleteMessagesFromMessageList(List<QuickType.Message> messageList, Action<int> messageDeleteCallback = null,
            Utils.RateLimitCallbackDelegate rateLimitCallback = null, CancellationToken ct = default)
        {
            int deletedCount = 0;

            messageList = messageList.Where(x => x.Author.Id == lastUserID).ToList();
            foreach (var message in messageList)
            {
                ct.ThrowIfCancellationRequested();
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

        public async Task DeleteMessagesFromMultipleChannels(ChannelAndGuild[] channelList,
            DeleteProgressCallbackDelegate progressCallback = null,
            Utils.RateLimitCallbackDelegate rateLimitCallback = null, CancellationToken ct = default)
        {
            List<Task> taskList = new List<Task>();
            int totalMessageCount = 0, deletedCount = 0;
            foreach (ChannelAndGuild channel in channelList)
            {
                var messageList = await GetAllMessagesByUserInChannel(channel, lastUserID, true,
                    rateLimitCallback, (int foundMessages) =>
                    {
                        progressCallback?.Invoke(channelList.Length, taskList.Count, totalMessageCount + foundMessages, deletedCount);
                    }, ct);

                totalMessageCount += messageList.ResultCount;

                taskList.Add(Task.Run(() => DeleteMessagesFromMessageList(messageList.MessageList, new Action<int>((int _deletedCount) =>
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
