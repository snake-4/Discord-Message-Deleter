using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Runtime.CompilerServices;

namespace Discord_Delete_Messages
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            outputRTBox.ReadOnly = true;
        }

        private void addLog(string text)
        {
            Console.WriteLine(text);
        }

        private void HandleError(string exceptionMessage, [CallerMemberName] string caller = "")
        {
            _ = MessageBox.Show("A problem occured in " + caller + "\n\nError:\n" + exceptionMessage
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

        private UInt64 deletedCount = 0;

        private async Task<List<QuickType.OnlyIDExtract>> get_user_GUILDs(string authId)
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    addLog("Getting user guilds...");
                    try
                    {
                        wc.Headers.Add(HttpRequestHeader.Authorization, authId);
                        string downloadedString;

                        downloadedString = await wc.DownloadStringTaskAsync("https://discordapp.com/api/v6/users/@me/guilds");

                        return QuickType.OnlyIDExtract.FromJsonList(downloadedString);
                    }
                    catch (Exception exc)
                    {
                        addLog("Failed! Error:" + exc.Message);
                        return null;
                    }
                }
            }
            catch (Exception exc)
            {
                HandleError(exc.Message);
            }
            return null;
        }
        private async Task<List<QuickType.DmChatGroup>> get_user_DMs(string authId)
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    addLog("Getting user dms...");
                    try
                    {
                        wc.Headers.Add(HttpRequestHeader.Authorization, authId);
                        string downloadedString;

                        downloadedString = await wc.DownloadStringTaskAsync("https://discordapp.com/api/v6/users/@me/channels");

                        return QuickType.DmChatGroup.FromJsonList(downloadedString);
                    }
                    catch (Exception exc)
                    {
                        addLog("Failed! Error:" + exc.Message);
                        return null;
                    }
                }
            }
            catch (Exception exc)
            {
                HandleError(exc.Message);
            }
            return null;
        }
        private async Task<QuickType.OnlyIDExtract> get_Current_user(string authId)
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    addLog("Getting current user...");
                    wc.Headers.Add(HttpRequestHeader.Authorization, authId);
                    string downloadedString;

                    downloadedString = await wc.DownloadStringTaskAsync("https://discordapp.com/api/v6/users/@me");

                    return QuickType.OnlyIDExtract.FromJson(downloadedString);
                }
            }
            catch (Exception exc)
            {
                HandleError(exc.Message);
            }
            return null;
        }
        private async Task<Search_Result_Struct> get_Search_Results(string authId, string channelId, string userId, string offset, bool isGuild)
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    string downloadedString;
                    addLog("Getting results...");
                    wc.Headers.Add(HttpRequestHeader.Authorization, authId);

                    if (!isGuild)
                    {
                        if (offset == "0")
                        {
                            downloadedString = await wc.DownloadStringTaskAsync($"https://discordapp.com/api/v6/channels/{channelId}/messages/search?author_id={userId}");
                        }
                        else
                        {
                            downloadedString = await wc.DownloadStringTaskAsync($"https://discordapp.com/api/v6/channels/{channelId}/messages/search?author_id={userId}&offset={offset}");
                        }
                    }
                    else
                    {
                        if (offset == "0")
                        {
                            downloadedString = await wc.DownloadStringTaskAsync($"https://discordapp.com/api/v6/guilds/{channelId}/messages/search?author_id={userId}");
                        }
                        else
                        {
                            downloadedString = await wc.DownloadStringTaskAsync($"https://discordapp.com/api/v6/guilds/{channelId}/messages/search?author_id={userId}&offset={offset}");
                        }
                    }

                    QuickType.SearchResult result = QuickType.SearchResult.FromJson(downloadedString);

                    Search_Result_Struct search_result = new Search_Result_Struct
                    {
                        TotalResults = result.TotalResults
                    };

                    List<QuickType.Message> allMessages = new List<QuickType.Message>();

                    foreach (List<QuickType.Message> messagesCHUNK in result.Messages)
                    {
                        foreach (QuickType.Message message in messagesCHUNK)
                        {
                            bool contains = false;
                            foreach (QuickType.Message messagetmp in search_result.messageList)
                            {
                                if (messagetmp.Id == message.Id)
                                {
                                    contains = true;
                                    break;
                                }
                            }

                            if (!contains)
                            {
                                search_result.messageList.Add(message);
                            }
                        }
                    }

                    return search_result;
                }
            }
            catch (Exception exc)
            {
                //HandleError(exc.Message);
            }
            return null;
        }

        private async Task<long> get_message_count(string authId, string userId, string channelId, bool isGuild, bool onlyNormalMessages)
        {
            try
            {
                Search_Result_Struct searchResult = await get_Search_Results(authId, channelId, userId, "0", isGuild);
                long MessageCount = 0;
                if (searchResult != null)
                {
                    if (onlyNormalMessages)
                    {
                        foreach (QuickType.Message message in searchResult.messageList)
                        {
                            if (message.Author.Id == userId && message.Type == 0)
                            {
                                MessageCount++;
                            }
                        }
                    }
                    else
                    {
                        foreach (QuickType.Message message in searchResult.messageList)
                        {
                            if (message.Author.Id == userId)
                            {
                                MessageCount++;
                            }
                        }
                    }
                }
                return MessageCount;
            }
            catch (Exception exc)
            {
                HandleError(exc.Message);
            }
            return 0;
        }

        private async Task Delete_From_Channel(string authId, string userId, string channelId, bool isGuild)
        {
            try
            {
                long offset = 0;
                while (true)
                {
                    Search_Result_Struct downloadedResult = null;
                    for (int i = 0; i < 12; i++) //Timeout: 2 minutes
                    {

                        downloadedResult = await get_Search_Results(authId, channelId, userId, offset.ToString(), isGuild);

                        if (downloadedResult == null || downloadedResult.messageList == null)
                        {
                            await Task.Delay(10000);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (downloadedResult == null || downloadedResult.messageList == null)
                    {
                        addLog("Can't get search results, fatal error!");
                        break;
                    }

                    if (offset < downloadedResult.messageList.Count)
                    {
                        offset += 25; // 1 chunk
                    }
                    else
                    {
                        if (await get_message_count(authId, userId, channelId, isGuild, true) == 0)
                        {
                            addLog("Finished!");
                            return;
                        }
                        else
                        {
                            offset = 0;
                        }
                    }


                    foreach (QuickType.Message message in downloadedResult.messageList)
                    {
                        if (message.Author.Id != userId || message.Type != 0)
                            continue;

                        addLog("Removing " + message.Id);
                        try
                        {
                            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"https://discordapp.com/api/v6/channels/{message.ChannelId}/messages/{message.Id}");
                            request.Method = "DELETE";
                            request.Headers.Add(HttpRequestHeader.Authorization, authId);
                            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK)
                            {
                                addLog("Deleted a message! " + message.Id);
                                deletedCount++;

                            }
                        }
                        catch (Exception exc)
                        {
                            addLog("Failed! Error:" + exc.Message);
                        }
                    }

                }
            }
            catch (Exception exc)
            {
                HandleError(exc.Message);
            }
        }

        private async Task Delete_From_ChannelID_List(string[] channelIDs, string authId, string userId, bool updateOutput)
        {
            try
            {
                if (updateOutput)
                {
                    outputRTBox.Text = "Downloading data... Please wait.";
                }

                long TotalMessageCount = 0;
                List<Task> TaskList = new List<Task>();
                List<Channel_Struct> channels = new List<Channel_Struct>();
                foreach (string channelID in channelIDs)
                {
                    Channel_Struct channel = new Channel_Struct();
                    channel.isGuild = channelID.StartsWith("G", true, null); ;
                    channel.channelID = channelID.ToUpper().Replace("G", "");

                    if (!string.IsNullOrWhiteSpace(channel.channelID) && !channels.Contains(channel))
                    {
                        channels.Add(channel);
                    }
                }

                foreach (Channel_Struct channel in channels)
                {
                    TotalMessageCount += await get_message_count(authId, userId, channel.channelID, channel.isGuild, true);
                    TaskList.Add(Task.Run(() => Delete_From_Channel(authId, userId, channel.channelID, channel.isGuild)));
                }

                int completed = 0;
                while (completed < TaskList.Count)
                {
                    completed = 0;
                    foreach (Task a in TaskList)
                    {
                        if (a.IsCompleted)
                        {
                            completed++;
                        }
                    }
                    if (updateOutput)
                    {
                        outputRTBox.Text = $"{completed} / {TaskList.Count} : {deletedCount} / {TotalMessageCount}";
                    }
                    Application.DoEvents();
                    await Task.Delay(100);
                }

                await Task.WhenAll(TaskList.ToArray());
            }
            catch (Exception exc)
            {
                HandleError(exc.Message);
            }
        }

        private async void startButton_Click(object sender, EventArgs e)
        {
            try
            {
                deletedCount = 0;
                startButton.Enabled = false;
                nukeButton.Enabled = false;
                nuke_DMS_CheckBox.Enabled = false;
                nuke_GUILDS_CheckBox.Enabled = false;
                channelIDsRTBox.Enabled = false;
                this.UseWaitCursor = true;
                Application.DoEvents();


                string authId = authID_TextBox.Text.Replace(" ", "").Replace("\"", "");
                if (string.IsNullOrWhiteSpace(authId))
                {
                    MessageBox.Show("Please fill auth ID!");
                    return;
                }
                string userId = (await get_Current_user(authId)).Id;
                string[] channelIds = channelIDsRTBox.Text.Replace(" ", "").Split(',');

                await Delete_From_ChannelID_List(channelIds, authId, userId, true);
            }
            catch (Exception exc)
            {
                HandleError(exc.Message);
            }
            finally
            {
                nuke_DMS_CheckBox.Enabled = true;
                nuke_GUILDS_CheckBox.Enabled = true;
                startButton.Enabled = true;
                nukeButton.Enabled = true;
                channelIDsRTBox.Enabled = true;
                this.UseWaitCursor = false;
            }
        }

        private void helpButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("If there are multiple channel ID(s), you can seperate them by using \",\"." + "\n" +
                "Add \"G\" in front of guild ID(s) and put them in channel ID(s) box too." + "\n" +
                "NUKE button deletes all messages from selected checkboxes." + "\n" +
                "P.S. GUILD means Discord server but they are reffered as GUILD in Discord API.");
        }

        private async void nukeButton_Click(object sender, EventArgs e)
        {
            try
            {
                deletedCount = 0;
                startButton.Enabled = false;
                nukeButton.Enabled = false;
                nuke_DMS_CheckBox.Enabled = false;
                nuke_GUILDS_CheckBox.Enabled = false;
                channelIDsRTBox.Enabled = false;
                this.UseWaitCursor = true;
                Application.DoEvents();
                bool nukeDMS = nuke_DMS_CheckBox.Checked;
                bool nukeGUILDS = nuke_GUILDS_CheckBox.Checked;

                string authID = authID_TextBox.Text.Replace(" ", "").Replace("\"", "");
                if (string.IsNullOrWhiteSpace(authID))
                {
                    MessageBox.Show("Please fill auth ID!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string userID = (await get_Current_user(authID)).Id;
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

                List<string> channelIds = new List<string>();
                if (nukeDMS)
                {
                    foreach (QuickType.DmChatGroup dmChat in await get_user_DMs(authID))
                    {
                        channelIds.Add(dmChat.Id);
                    }
                }

                if (nukeGUILDS)
                {
                    foreach (QuickType.OnlyIDExtract onlyGUILD in await get_user_GUILDs(authID))
                    {
                        channelIds.Add("G" + onlyGUILD.Id);
                    }
                }

                await Delete_From_ChannelID_List(channelIds.ToArray(), authID, userID, true);
            }
            catch (Exception exc)
            {
                HandleError(exc.Message);
            }
            finally
            {
                nuke_DMS_CheckBox.Enabled = true;
                nuke_GUILDS_CheckBox.Enabled = true;
                startButton.Enabled = true;
                nukeButton.Enabled = true;
                channelIDsRTBox.Enabled = true;
                this.UseWaitCursor = false;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            MessageBox.Show("Total count of message counter is broken but don't worry, the program works fine.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
