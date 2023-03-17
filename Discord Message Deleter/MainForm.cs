using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

#pragma warning disable IDE0063 // Disable "Use simple 'using' statement" because we don't want to use C# 8.0 yet.

namespace DiscordMessageDeleter
{
    public partial class MainForm : Form
    {
        DiscordAPI discordAPI = new DiscordAPI();
        CancellationTokenSource cts = new CancellationTokenSource();

        public MainForm()
        {
            InitializeComponent();
        }

        private async Task handle_startButton_Click()
        {
            try
            {
                cts = new CancellationTokenSource();
                searchedChannelsLabel.Text = "N/A";
                foundMessagesLabel.Text = "N/A";
                foundMessagesLabel.Text = "N/A";
                toolStripProgressBar.Value = 0;

                bool nukeDMS = nuke_DMS_CheckBox.Checked;
                bool nukeGUILDS = nuke_GUILDS_CheckBox.Checked;

                string warningMessage = $"This operation will delete all messages in " +
                    $"{(nukeDMS ? "DM(s)" : "")}{(nukeGUILDS && nukeDMS ? " and " : "")}{(nukeGUILDS ? "guild(s)" : "")}. Are you sure you want to continue?";
                if ((nukeDMS || nukeGUILDS) && MessageBox.Show(warningMessage, "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                {
                    toolStripStatusText.Text = "Cancelled!";
                    return;
                }

                toolStripStatusText.Text = "Fetching information...";
                await UpdateAuthIDFromUI();

                var finalIDList = await BuildIDListFromUI(nukeDMS, nukeGUILDS);

                await discordAPI.DeleteMessagesFromMultipleChannels(finalIDList.ToArray(),
                    (int allChannelCount, int searchedChannelCount, int foundMessageCount, int deletedMessageCount) => this.InvokeIfRequired(() =>
                    {
                        foundMessagesLabel.Text = $"{deletedMessageCount} / {foundMessageCount}";
                        searchedChannelsLabel.Text = $"{searchedChannelCount} / {allChannelCount}";

                        //This callback will also be called when new messages are found, so the deleted message count can be zero
                        //so we have to prevent the divide by zero here
                        if (deletedMessageCount > 0)
                        {
                            toolStripProgressBar.Value = (int)((float)deletedMessageCount / foundMessageCount * toolStripProgressBar.Maximum);
                            toolStripStatusText.Text = "Deleting... ";
                        }
                        else
                        {
                            toolStripStatusText.Text = "Searching messages...";
                        }
                    }),
                    (int rlSeconds) => this.InvokeIfRequired(() => toolStripStatusText.Text = $"Ratelimited for {rlSeconds}s!")
                    , cts.Token);

                toolStripStatusText.Text = "Finished!";
            }
            catch (Exception exc)
            {
                if (exc is TaskCanceledException || exc is OperationCanceledException)
                {
                    MessageBox.Show("Operation cancelled.");
                    toolStripStatusText.Text = "Cancelled!";
                }
                else
                {
                    MessageBox.Show(exc.Message, "Encountered an error!");
                    toolStripStatusText.Text = "Errored!";
                }
            }
        }

        private async Task<IEnumerable<DiscordAPI.ChannelAndGuild>> BuildIDListFromUI(bool EraseAllDMs, bool EraseAllGuilds)
        {
            var idTextboxInput = channelIDsRTBox.Text.Replace(" ", "").Split(',');
            var userIDList = idTextboxInput.Where(x => x.StartsWith("U")).Select(x => x.Substring(1)).Distinct();
            var guildIDList = idTextboxInput.Where(x => x.StartsWith("G")).Select(x => x.Substring(1)).Distinct();
            var channelIDList = idTextboxInput.Where(x => x.All(char.IsDigit)).Distinct();

            var allDMsList = await discordAPI.GetUserDMList(null, cts.Token);
            var allGuildsList = await discordAPI.GetUserGuilds(null, cts.Token);
            var finalIDList = new List<DiscordAPI.ChannelAndGuild>();

            if (EraseAllGuilds)
                finalIDList.AddRange(allGuildsList.Select(x => new DiscordAPI.ChannelAndGuild(x.Id, true)));
            else
                finalIDList.AddRange(guildIDList.Select(x => new DiscordAPI.ChannelAndGuild(x, true)));

            if (EraseAllDMs)
                finalIDList.AddRange(allDMsList.Select(x => new DiscordAPI.ChannelAndGuild(x.Id, false)));
            else
                finalIDList.AddRange(allDMsList.Where((QuickType.DmChatGroup dm) =>
                {
                    return dm.Recipients != null && dm.Recipients.Count() == 1 && userIDList.Contains(dm.Recipients.First().Id);
                }).Select(x => new DiscordAPI.ChannelAndGuild(x.Id, false)));

            finalIDList.AddRange(channelIDList.Select(x => new DiscordAPI.ChannelAndGuild(x, false)));

            return finalIDList.Distinct();
        }

        private async Task UpdateAuthIDFromUI()
        {
            string authID = authID_TextBox.Text.Replace(" ", "").Replace("\"", "");
            if (string.IsNullOrWhiteSpace(authID))
            {
                throw new Exception("Please fill the Auth ID text box!");
            }
            await discordAPI.SetAuthID(authID);
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            cts.Cancel();
        }

        private async void startButton_Click(object sender, EventArgs e)
        {
            FreezeUI();
            await handle_startButton_Click();
            UnfreezeUI();
        }

        private void helpButton_Click(object sender, EventArgs e) =>
            MessageBox.Show("If there are multiple channel IDs, you can seperate them using a comma(\",\")." + "\n" +
                "Add \"G\" in front of guild IDs." + "\n" +
                "Add \"U\" in front of user IDs." + "\n");

        private void FreezeUI()
        {
            authID_TextBox.Enabled = false;
            startButton.Enabled = false;
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
            channelIDsRTBox.Enabled = true;
            UseWaitCursor = false;
            Application.DoEvents();
        }

        private void aboutButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Made by https://github.com/SnakePin." + Environment.NewLine + "Version: GIT");
        }
    }
}
