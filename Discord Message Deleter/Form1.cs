using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Net.Http;
using System.Net;
using System.IO;

#pragma warning disable IDE0063 // Disable "Use simple 'using' statement" because we don't want to use C# 8.0 yet.

namespace Discord_Delete_Messages
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            outputRTBox.ReadOnly = true;
        }

        private void AddLogLine(string text) => Console.WriteLine(text);

        private void ShowErrorAndExit(string exceptionMessage, [CallerMemberName] string caller = "")
        {
            MessageBox.Show("A problem occured in " + caller + "\n\nError:\n" + exceptionMessage
                + "\n\nPlease report this to developer! Exiting now.",
                "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
        }

        class Channel_Struct
        {
            public Channel_Struct()
            {
                isGuild = false;
                channelID = string.Empty;
            }

            public bool isGuild;
            public string channelID;
        }
        class DiscordSearchResult
        {
            public DiscordSearchResult()
            {
                messageList = new List<QuickType.Message>();
                TotalResults = 0;
            }

            public List<QuickType.Message> messageList;
            public long TotalResults;
        }

        static readonly HttpClient httpClient = new HttpClient();

        const string discordApiUrl = "https://discordapp.com/api/v6/";

        private async Task<HttpResponseMessage> HttpRequestAndWaitRatelimit(HttpRequestMessage request)
        {
            //request.Headers.Add("X-RateLimit-Precision", "millisecond");

            while (true)
            {
                var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }
                else if ((int)response.StatusCode == 429)
                {
                    //UInt32 rateLimitResetTime = UInt32.Parse(response.Headers.GetValues("X-RateLimit-Reset-After").First().Replace(".", ""));
                    UInt32 rateLimitResetTime = UInt32.Parse(response.Headers.GetValues("Retry-After").First());
                    await Task.Delay(TimeSpan.FromMilliseconds(rateLimitResetTime));
                    request = await request.CloneAsync();
                }
                else
                {
                    throw new HttpRequestException();
                }
            }
        }
        private async Task<string> HttpGetStringAndWaitRatelimit(Uri uri) => await (await HttpRequestAndWaitRatelimit(new HttpRequestMessage(HttpMethod.Get, uri))).Content.ReadAsStringAsync();
        private async Task<string> HttpGetStringAndWaitRatelimit(string url) => await HttpGetStringAndWaitRatelimit(new Uri(url));
        private async Task<List<QuickType.OnlyIDExtract>> GetUsersGuilds() => QuickType.OnlyIDExtract.FromJsonList(await HttpGetStringAndWaitRatelimit(discordApiUrl + "users/@me/guilds"));
        private async Task<List<QuickType.DmChatGroup>> GetUsersDmList() => QuickType.DmChatGroup.FromJsonList(await HttpGetStringAndWaitRatelimit(discordApiUrl + "users/@me/channels"));
        private async Task<QuickType.OnlyIDExtract> GetUserIDByAuthID() => QuickType.OnlyIDExtract.FromJson(await HttpGetStringAndWaitRatelimit(discordApiUrl + "users/@me"));

        private async Task<DiscordSearchResult> GetMessagesByUserInChannelByOffset(string channelId, string userId, int offset, bool isGuild)
        {

            string targetRestUrl = "/" + (isGuild ? "guilds" : "channels");
            targetRestUrl += $"/{channelId}/messages/search?author_id={userId}";
            targetRestUrl += (offset != 0) ? $"&offset={offset}" : "";

            QuickType.SearchResult result = QuickType.SearchResult.FromJson(await HttpGetStringAndWaitRatelimit(discordApiUrl + targetRestUrl));

            DiscordSearchResult search_result = new DiscordSearchResult
            {
                TotalResults = result.TotalResults
            };
            search_result.messageList.AddRange(result.Messages.SelectMany(messageChunk => messageChunk.Where(message => message.Author.Id == userId).Select(message => message)).DistinctBy(x => x.Id));
            return search_result;
        }

        private async Task<DiscordSearchResult> GetAllMessagesByUserInChannel(string channelId, string userId, bool isGuild, bool onlyNormalMessages)
        {
            var returnValueResults = new DiscordSearchResult();

            int currentOffset = 0;

            while (currentOffset <= 5000)
            {
                var currentSearchResults = await GetMessagesByUserInChannelByOffset(channelId, userId, currentOffset, isGuild);
                returnValueResults.TotalResults = currentSearchResults.TotalResults;

                int currentMessageCount = currentSearchResults.messageList.Count;
                if (currentMessageCount == 0)
                {
                    break;
                }

                currentOffset += currentMessageCount;
                returnValueResults.messageList.AddRange(currentSearchResults.messageList);
            }

            if (onlyNormalMessages)
            {
                returnValueResults.messageList = returnValueResults.messageList.Where(x => x.Type == 0).ToList();
            }

            //We have to be sure that there are no duplicates
            returnValueResults.messageList = returnValueResults.messageList.DistinctBy(x => x.Id).ToList();

            return returnValueResults;
        }

        private async Task DeleteMessagesFromMessageList(string userId, List<QuickType.Message> messageList, IProgress<long> progress = null)
        {
            long deletedCount = 0;

            messageList = messageList.Where(x => x.Author.Id == userId).ToList();
            foreach (var message in messageList)
            {
                try
                {
                    HttpRequestMessage request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Delete,
                        RequestUri = new Uri(discordApiUrl + $"channels/{message.ChannelId}/messages/{message.Id}")
                    };

                    var response = await HttpRequestAndWaitRatelimit(request);

                    AddLogLine("Deleted a message! " + message.Id);

                    if (progress != null)
                    {
                        progress.Report(++deletedCount);
                    }
                }
                catch (Exception exc)
                {
                    AddLogLine("Failed! Error:" + exc.Message);
                }
            }
        }

        private async Task DeleteMessagesFromMultipleChannels(string[] channelIDs, string userId, bool updateOutput)
        {
            try
            {
                if (updateOutput)
                {
                    outputRTBox.Text = "Downloading data... Please wait.";
                }

                var channelList = new List<Channel_Struct>();
                foreach (string channelID in channelIDs)
                {
                    //TODO: add feature to delete in DM channel by user ID

                    Channel_Struct channel = new Channel_Struct
                    {
                        isGuild = channelID.StartsWith("G", true, null),
                        channelID = channelID.ToUpper().Replace("G", "")
                    };

                    if (!string.IsNullOrWhiteSpace(channel.channelID))
                    {
                        channelList.Add(channel);
                    }
                }

                //Remove duplicates
                channelList = channelList.Distinct().ToList();

                List<Task> taskList = new List<Task>();
                long totalMessageCount = 0, deletedCount = 0;
                foreach (Channel_Struct channel in channelList)
                {
                    var messageList = (await GetAllMessagesByUserInChannel(channel.channelID, userId, channel.isGuild, true)).messageList;
                    totalMessageCount += messageList.Count;
                    taskList.Add(Task.Run(() => DeleteMessagesFromMessageList(userId, messageList, new Progress<long>(_deletedCount =>
                    {
                        deletedCount = _deletedCount;
                    }))));
                }

                Task whenAllTask = Task.WhenAll(taskList);
                do
                {
                    if (updateOutput)
                    {
                        int completed = taskList.Where(x => x.IsCompleted).Count();
                        outputRTBox.Text = $"{completed} / {taskList.Count} : {deletedCount} / {totalMessageCount}";
                    }
                    await Task.Delay(100);
                }
                while (!whenAllTask.IsCompleted);
            }
            catch (Exception exc)
            {
                ShowErrorAndExit(exc.Message);
            }
        }

        private async void startButton_Click(object sender, EventArgs e)
        {
            try
            {
                FreezeUI();
                string authID = authID_TextBox.Text.Replace(" ", "").Replace("\"", "");
                if (string.IsNullOrWhiteSpace(authID))
                {
                    MessageBox.Show("Please fill AuthID!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                httpClient.DefaultRequestHeaders.Remove("Authorization");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authID);

                AddLogLine("Getting current user...");
                string userID = (await GetUserIDByAuthID()).Id;
                if (userID == null)
                {
                    MessageBox.Show("Make sure AuthID is correct!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string[] channelIds = channelIDsRTBox.Text.Replace(" ", "").Split(',');

                await DeleteMessagesFromMultipleChannels(channelIds, userID, true);
            }
            catch (Exception exc)
            {
                ShowErrorAndExit(exc.Message);
            }
            UnfreezeUI();
        }

        private async void nukeButton_Click(object sender, EventArgs e)
        {
            try
            {
                FreezeUI();
                bool nukeDMS = nuke_DMS_CheckBox.Checked;
                bool nukeGUILDS = nuke_GUILDS_CheckBox.Checked;

                string authID = authID_TextBox.Text.Replace(" ", "").Replace("\"", "");
                if (string.IsNullOrWhiteSpace(authID))
                {
                    MessageBox.Show("Please fill AuthID!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                httpClient.DefaultRequestHeaders.Remove("Authorization");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authID);

                AddLogLine("Getting current user...");
                string userID = (await GetUserIDByAuthID()).Id;
                if (userID == null)
                {
                    MessageBox.Show("Make sure AuthID is correct!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!nukeDMS && !nukeGUILDS)
                {
                    MessageBox.Show("Select atleast one checkbox to nuke!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (MessageBox.Show("This operation will delete all messages in "
                    + (nukeDMS ? "DM(s)" : "") + (nukeGUILDS && nukeDMS ? " and from " : "") + (nukeGUILDS ? "GUILD(s)" : "") + ". " +
                    "Are you sure you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                {
                    return;
                }

                var channelIds = new List<string>();

                if (nukeDMS)
                {
                    channelIds.AddRange((await GetUsersDmList()).Select(dmChat => dmChat.Id));
                }
                if (nukeGUILDS)
                {
                    channelIds.AddRange((await GetUsersGuilds()).Select(onlyGUILD => "G" + onlyGUILD.Id));
                }

                await DeleteMessagesFromMultipleChannels(channelIds.ToArray(), userID, true);
            }
            catch (Exception exc)
            {
                ShowErrorAndExit(exc.Message);
            }
            UnfreezeUI();
        }

        private void helpButton_Click(object sender, EventArgs e) =>
            MessageBox.Show("If there are multiple channel IDs, you can seperate them by using \",\"(comma)." + "\n" +
                "Add \"G\" in front of guild IDs and put them in \"Channel ID(s)\" textbox too." + "\n" +
                "NUKE button deletes all messages from DMs or guilds according to the checkboxes selected." + "\n" +
                "P.S. Guild means Discord server.");

        private void FreezeUI()
        {
            authID_TextBox.Enabled = false;
            startButton.Enabled = false;
            nukeButton.Enabled = false;
            nuke_DMS_CheckBox.Enabled = false;
            nuke_GUILDS_CheckBox.Enabled = false;
            channelIDsRTBox.Enabled = false;
            UseWaitCursor = true;
            Application.DoEvents();
        }

        private void UnfreezeUI()
        {
            authID_TextBox.Enabled = true;
            nuke_DMS_CheckBox.Enabled = true;
            nuke_GUILDS_CheckBox.Enabled = true;
            startButton.Enabled = true;
            nukeButton.Enabled = true;
            channelIDsRTBox.Enabled = true;
            UseWaitCursor = false;
            Application.DoEvents();
        }

        private void aboutButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Made by https://github.com/SnakePin." + Environment.NewLine + "Version: " + ProductVersion);
            //TODO: show about form here
        }
    }

    public static class Extensions
    {
        public static async Task<HttpRequestMessage> CloneAsync(this HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Content = await request.Content.CloneAsync().ConfigureAwait(false),
                Version = request.Version
            };
            foreach (KeyValuePair<string, object> prop in request.Properties)
            {
                clone.Properties.Add(prop);
            }
            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }

        public static async Task<HttpContent> CloneAsync(this HttpContent content)
        {
            if (content == null) return null;

            var ms = new MemoryStream();
            await content.CopyToAsync(ms).ConfigureAwait(false);
            ms.Position = 0;

            var clone = new StreamContent(ms);
            foreach (KeyValuePair<string, IEnumerable<string>> header in content.Headers)
            {
                clone.Headers.Add(header.Key, header.Value);
            }
            return clone;
        }
        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> property)
        {
            GeneralPropertyComparer<T, TKey> comparer = new GeneralPropertyComparer<T, TKey>(property);
            return items.Distinct(comparer);
        }
        public class GeneralPropertyComparer<T, TKey> : IEqualityComparer<T>
        {
            private Func<T, TKey> expr { get; set; }
            public GeneralPropertyComparer(Func<T, TKey> expr)
            {
                this.expr = expr;
            }
            public bool Equals(T left, T right)
            {
                var leftProp = expr.Invoke(left);
                var rightProp = expr.Invoke(right);
                if (leftProp == null && rightProp == null)
                    return true;
                else if (leftProp == null ^ rightProp == null)
                    return false;
                else
                    return leftProp.Equals(rightProp);
            }
            public int GetHashCode(T obj)
            {
                var prop = expr.Invoke(obj);
                return (prop == null) ? 0 : prop.GetHashCode();
            }
        }
    }
}
