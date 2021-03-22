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

        private void ShowErrorAndExit(string exceptionMessage, [CallerMemberName] string caller = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            MessageBox.Show($"A problem occured in {caller} at line {sourceLineNumber}\n\nError:\n{exceptionMessage}\n\nPlease report this to the developer.",
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

        const string discordApiUrl = "https://discordapp.com/api/v8/";

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
                    string excStr = "Exception in HttpRequestAndWaitRatelimit: response.StatusCode is " + response.StatusCode;
                    if (response.Headers.TryGetValues("X-RateLimit-Remaining", out IEnumerable<string> XRateLimitRemainingValues))
                    {
                        excStr += " X-RateLimit-Remaining is " + XRateLimitRemainingValues.First();
                    }
                    string body = null;
                    if ((body = await response.Content.ReadAsStringAsync()) != null)
                    {
                        excStr += " response.Content is " + body;
                    }
                    throw new Exception(excStr);
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

            QuickType.SearchResult result = null;

            while (result == null || result.Messages == null)
            {
                result = QuickType.SearchResult.FromJson(await HttpGetStringAndWaitRatelimit(discordApiUrl + targetRestUrl));
            }

            DiscordSearchResult search_result = new DiscordSearchResult
            {
                TotalResults = result.TotalResults
            };

            search_result.messageList.AddRange(result.Messages.SelectMany(messageChunk => messageChunk.Where(message => message.Author.Id == userId)).DistinctBy(x => x.Id));
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
                returnValueResults.messageList = returnValueResults.messageList.Where(x => x.Type == 0 || x.Type == 19 || x.Type == 20).ToList();
            }

            //We have to be sure that there are no duplicates
            returnValueResults.messageList = returnValueResults.messageList.DistinctBy(x => x.Id).ToList();

            returnValueResults.TotalResults = returnValueResults.messageList.Count;

            return returnValueResults;
        }

        private async Task DeleteMessagesFromMessageList(string userId, List<QuickType.Message> messageList, Action<long> messageDeleteCallback = null)
        {
            long deletedCount = 0;

            messageList = messageList.Where(x => x.Author.Id == userId).ToList();
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

                        var response = await HttpRequestAndWaitRatelimit(request);
                    }

                    AddLogLine("Deleted a message! " + message.Id);

                    messageDeleteCallback?.Invoke(++deletedCount);
                }
                catch (Exception exc)
                {
                    AddLogLine("Failed deleting a message! Error:" + exc.Message);
                }
            }
        }

        private async Task DeleteMessagesFromMultipleChannels(string[] channelIDs, string userId)
        {
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

            outputRTBox.Text = "Downloading data... Please wait.";
            AddLogLine(outputRTBox.Text);

            List<Task> taskList = new List<Task>();
            long totalMessageCount = 0, deletedCount = 0;
            foreach (Channel_Struct channel in channelList)
            {
                var messageList = await GetAllMessagesByUserInChannel(channel.channelID, userId, channel.isGuild, true);
                totalMessageCount += messageList.TotalResults;
                taskList.Add(Task.Run(() => DeleteMessagesFromMessageList(userId, messageList.messageList, new Action<long>((long _deletedCount) =>
                {
                    deletedCount++;
                    //We must access the controls from UI thread so we invoke this lambda from the UI thread
                    this.Invoke(new Action(() => outputRTBox.Text = $"{taskList.Count(x => x.IsCompleted)} / {taskList.Count} : {deletedCount} / {totalMessageCount}"));
                }))));
            }

            await Task.WhenAll(taskList);

            outputRTBox.Text = $"Deleted {deletedCount} message(s) from {taskList.Count} channel(s).";
            AddLogLine(outputRTBox.Text);
        }

        private async Task<string> GetUserIDAndSetAuthorizationHeader()
        {
            string authID = authID_TextBox.Text.Replace(" ", "").Replace("\"", "");
            if (string.IsNullOrWhiteSpace(authID))
            {
                MessageBox.Show("Please fill AuthID!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                throw new Exception();
            }
            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Add("Authorization", authID);
            outputRTBox.Text = "Downloading information about the current user...";
            AddLogLine(outputRTBox.Text);
            string userID;
            try
            {
                if ((userID = (await GetUserIDByAuthID()).Id) == null)
                {
                    throw new Exception();
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("Make sure AuthID is correct!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                throw exc;
            }

            return userID;
        }

        private async void startButton_Click(object sender, EventArgs e)
        {
            FreezeUI();
            string userID;
            try
            {
                userID = await GetUserIDAndSetAuthorizationHeader();
            }
            catch { UnfreezeUI(); return; }
            List<string> channelIds = channelIDsRTBox.Text.Replace(" ", "").Split(',').ToList();

            var dmList = await GetUsersDmList();
            foreach (var currentUserID in channelIds.Where(x => x.StartsWith("U")).Select(y => y.Substring(1)).ToList())
            {
                string channelID = dmList.FirstOrDefault(dm =>
                {
                    if (dm.Recipients == null || dm.Recipients.Count() != 1)
                    {
                        return false;
                    }
                    return dm.Recipients.First().Id == currentUserID;
                })?.Id;

                if (channelID == null)
                {
                    continue;
                }

                channelIds.Add(channelID);
            }

            channelIds.RemoveAll(x => x.StartsWith("U"));

            try
            {
                await DeleteMessagesFromMultipleChannels(channelIds.ToArray(), userID);
            }
            catch (Exception exc)
            {
                ShowErrorAndExit(exc.Message);
            }
            UnfreezeUI();
        }

        private async void nukeButton_Click(object sender, EventArgs e)
        {
            FreezeUI();
            bool nukeDMS = nuke_DMS_CheckBox.Checked;
            bool nukeGUILDS = nuke_GUILDS_CheckBox.Checked;

            string userID;
            try
            {
                userID = await GetUserIDAndSetAuthorizationHeader();
            }
            catch { UnfreezeUI(); return; }

            if (!nukeDMS && !nukeGUILDS)
            {
                MessageBox.Show("Please select at least one of the checkboxes to nuke!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                UnfreezeUI(); return;
            }

            if (MessageBox.Show("This operation will delete all messages in "
                + (nukeDMS ? "DM(s)" : "") + (nukeGUILDS && nukeDMS ? " and from " : "") + (nukeGUILDS ? "GUILD(s)" : "") + ". " +
                "Are you sure you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
            {
                UnfreezeUI(); return;
            }

            var channelIds = new List<string>();

            if (nukeDMS)
            {
                channelIds.AddRange((await GetUsersDmList()).Select(dmChat => dmChat.Id));
            }
            if (nukeGUILDS)
            {
                channelIds.AddRange((await GetUsersGuilds()).Select(guildInstance => "G" + guildInstance.Id));
            }

            try
            {
                await DeleteMessagesFromMultipleChannels(channelIds.ToArray(), userID);
            }
            catch (Exception exc)
            {
                ShowErrorAndExit(exc.Message);
            }
            UnfreezeUI();
        }

        private void helpButton_Click(object sender, EventArgs e) =>
            MessageBox.Show("If there are multiple channel IDs, you can seperate them by using \",\"(comma)." + "\n" +
                "Add \"G\" in front of guild IDs." + "\n" +
                "Add \"U\" in front of user IDs." + "\n" +
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
        internal class GeneralPropertyComparer<T, TKey> : IEqualityComparer<T>
        {
            private Func<T, TKey> expr { get; set; }
            internal GeneralPropertyComparer(Func<T, TKey> expr)
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
