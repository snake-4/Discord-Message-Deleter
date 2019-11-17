using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;

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

        private void AddLogLine(string text)
        {
            Console.WriteLine(text);
        }

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

        class Search_Result_Struct
        {
            public Search_Result_Struct()
            {
                messageList = new List<QuickType.Message>();
                TotalResults = 0;
            }

            public List<QuickType.Message> messageList;
            public long TotalResults;
        }

        private ulong deletedCount = 0;

        static readonly HttpClient httpClient = new HttpClient()
        {
            BaseAddress = new Uri("https://discordapp.com/api/v6/"),
        };

        private async Task<List<QuickType.OnlyIDExtract>> GetUsersGuilds(string authId)
        {
            try
            {
                return QuickType.OnlyIDExtract.FromJsonList(await httpClient.GetStringAsync("users/@me/guilds"));
            }
            catch (Exception exc)
            {
                ShowErrorAndExit(exc.Message);
            }
            return null;
        }

        private async Task<List<QuickType.DmChatGroup>> GetUsersDmList(string authId)
        {
            try
            {
                return QuickType.DmChatGroup.FromJsonList(await httpClient.GetStringAsync("users/@me/channels"));
            }
            catch (Exception exc)
            {
                ShowErrorAndExit(exc.Message);
            }
            return null;
        }

        private async Task<QuickType.OnlyIDExtract> GetUserIDByAuthID(string authId)
        {
            try
            {
                AddLogLine("Getting current user...");
                return QuickType.OnlyIDExtract.FromJson(await httpClient.GetStringAsync("users/@me"));
            }
            catch (Exception exc)
            {
                ShowErrorAndExit(exc.Message);
            }
            return null;
        }

        private async Task<Search_Result_Struct> GetMessagesByUserInChannelByOffset(string channelId, string userId, string offset, bool isGuild)
        {

            string targetRestUrl = (isGuild ? "guilds" : "channels");
            targetRestUrl += $"/{channelId}/messages/search?author_id={userId}";
            targetRestUrl += (offset != "0") ? $"&offset={offset}" : "";

            string responseJson = await httpClient.GetStringAsync(targetRestUrl);

            QuickType.SearchResult result = QuickType.SearchResult.FromJson(responseJson);

            var search_result = new Search_Result_Struct
            {
                TotalResults = result.TotalResults
            };

            foreach (var messageChunk in result.Messages)
            {
                foreach (var message in messageChunk)
                {
                    if (message.Author.Id == userId && !search_result.messageList.Exists(x => x.Id == message.Id))
                    {
                        search_result.messageList.Add(message);
                    }
                }
            }

            return search_result;
        }

        private async Task<Search_Result_Struct> GetAllMessagesByUserInChannel(string channelId, string userId, bool isGuild, bool onlyNormalMessages)
        {
            var returnValueResults = new Search_Result_Struct();

            int currentOffset = 0;

            while (currentOffset <= 5000)
            {
                var currentSearchResults = await GetMessagesByUserInChannelByOffset(channelId, userId, currentOffset.ToString(), isGuild);
                returnValueResults.TotalResults = currentSearchResults.TotalResults;

                int currentMessageCount = currentSearchResults.messageList.Count;
                if (currentMessageCount == 0)
                {
                    break;
                }

                currentOffset += currentMessageCount;
                foreach (var message in currentSearchResults.messageList)
                {
                    if (!returnValueResults.messageList.Exists(x => x.Id == message.Id))
                    {
                        returnValueResults.messageList.Add(message);
                    }
                }
            }

            if (onlyNormalMessages)
            {
                returnValueResults.messageList = returnValueResults.messageList.Where(x => x.Type == 0).ToList();
            }

            return returnValueResults;
        }

        private async Task DeleteMessagesFromMessageList(string userId, List<QuickType.Message> messageList)
        {
            try
            {
                messageList = messageList.Where(x => x.Type == 0 && x.Author.Id == userId).ToList();
                foreach (var message in messageList)
                {
                START_DELETE_MESSAGE:
                    AddLogLine("Removing " + message.Id);
                    try
                    {
                        HttpRequestMessage request = new HttpRequestMessage
                        {
                            Method = HttpMethod.Delete,
                            RequestUri = new Uri(httpClient.BaseAddress.ToString() + $"channels/{message.ChannelId}/messages/{message.Id}")
                        };
                        var response = await httpClient.SendAsync(request);

                        if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK)
                        {
                            AddLogLine("Deleted a message! " + message.Id);
                            deletedCount++;
                            continue;
                        }
                    }
                    catch (Exception exc)
                    {
                        AddLogLine("Failed! Error:" + exc.Message);
                    }
                    await Task.Delay(1000);
                    goto START_DELETE_MESSAGE;
                }

                AddLogLine("Finished!");
            }
            catch (Exception exc)
            {
                ShowErrorAndExit(exc.Message);
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

                deletedCount = 0;
                long totalMessageCount = 0;
                List<Task> taskList = new List<Task>();
                var channelList = new List<Channel_Struct>();
                foreach (string channelID in channelIDs)
                {
                    //TODO: add feature to delete in DM channel by user ID

                    Channel_Struct channel = new Channel_Struct
                    {
                        isGuild = channelID.StartsWith("G", true, null),
                        channelID = channelID.ToUpper().Replace("G", "")
                    };

                    if (!string.IsNullOrWhiteSpace(channel.channelID) && !channelList.Contains(channel))
                    {
                        channelList.Add(channel);
                    }
                }

                foreach (Channel_Struct channel in channelList)
                {
                    var messageList = (await GetAllMessagesByUserInChannel(channel.channelID, userId, channel.isGuild, true)).messageList;
                    totalMessageCount += messageList.Count;
                    taskList.Add(Task.Run(() => DeleteMessagesFromMessageList(userId, messageList)));
                }

                var whenAllTask = Task.WhenAll(taskList);
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

                string userID = (await GetUserIDByAuthID(authID)).Id;
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

        private void helpButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("If there are multiple channel IDs, you can seperate them by using \",\"(comma)." + "\n" +
                "Add \"G\" in front of guild IDs and put them in \"Channel ID(s)\" textbox too." + "\n" +
                "NUKE button deletes all messages from DMs or guilds according to the checkboxes selected." + "\n" +
                "P.S. Guild means Discord server.");
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

                string userID = (await GetUserIDByAuthID(authID)).Id;
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
                    foreach (QuickType.DmChatGroup dmChat in await GetUsersDmList(authID))
                    {
                        channelIds.Add(dmChat.Id);
                    }
                }
                if (nukeGUILDS)
                {
                    foreach (QuickType.OnlyIDExtract onlyGUILD in await GetUsersGuilds(authID))
                    {
                        channelIds.Add("G" + onlyGUILD.Id);
                    }
                }

                await DeleteMessagesFromMultipleChannels(channelIds.ToArray(), userID, true);
            }
            catch (Exception exc)
            {
                ShowErrorAndExit(exc.Message);
            }
            UnfreezeUI();
        }

        private void FreezeUI()
        {
            startButton.Enabled = false;
            nukeButton.Enabled = false;
            nuke_DMS_CheckBox.Enabled = false;
            nuke_GUILDS_CheckBox.Enabled = false;
            channelIDsRTBox.Enabled = false;
            this.UseWaitCursor = true;
            Application.DoEvents();
        }

        private void UnfreezeUI()
        {
            nuke_DMS_CheckBox.Enabled = true;
            nuke_GUILDS_CheckBox.Enabled = true;
            startButton.Enabled = true;
            nukeButton.Enabled = true;
            channelIDsRTBox.Enabled = true;
            this.UseWaitCursor = false;
            Application.DoEvents();
        }

        private void aboutButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Made by https://github.com/SnakePin");
            //TODO: show about form here
        }
    }
}
