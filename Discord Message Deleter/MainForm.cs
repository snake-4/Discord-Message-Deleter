using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;
using System.Net.Http;
using System.Threading;

#pragma warning disable IDE0063 // Disable "Use simple 'using' statement" because we don't want to use C# 8.0 yet.

namespace Discord_Delete_Messages
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        DiscordAPI discordAPI = new DiscordAPI();
        CancellationTokenSource cts = new CancellationTokenSource();

        private async Task handle_startButton_Click()
        {
            try
            {
                searchedChannelsLabel.Text = "N/A";
                foundMessagesLabel.Text = "N/A";
                foundMessagesLabel.Text = "N/A";
                toolStripProgressBar.Value = 0;
                cts = new CancellationTokenSource();

                toolStripStatusText.Text = "Fetching information...";

                string authID = authID_TextBox.Text.Replace(" ", "").Replace("\"", "");
                if (string.IsNullOrWhiteSpace(authID))
                {
                    throw new Exception("AuthID is not correct!");
                }

                await discordAPI.SetAuthID(authID);

                List<string> channelIds = channelIDsRTBox.Text.Replace(" ", "").Split(',').ToList();

                bool nukeDMS = nuke_DMS_CheckBox.Checked;
                bool nukeGUILDS = nuke_GUILDS_CheckBox.Checked;
                if (nukeDMS || nukeGUILDS)
                {
                    if (MessageBox.Show("This operation will delete all messages in "
                    + (nukeDMS ? "DM(s)" : "") + (nukeGUILDS && nukeDMS ? " and from " : "") + (nukeGUILDS ? "GUILD(s)" : "") + ". " +
                    "Are you sure you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    {
                        toolStripStatusText.Text = "Cancelled!";
                        return;
                    }
                    if (nukeDMS)
                    {
                        channelIds.AddRange((await discordAPI.GetUsersDmList(null, cts.Token)).Select(x => x.Id));
                    }
                    if (nukeGUILDS)
                    {
                        channelIds.AddRange((await discordAPI.GetUsersGuilds(null, cts.Token)).Select(x => "G" + x.Id));
                    }
                }

                var dmList = await discordAPI.GetUsersDmList();
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
                channelIds.RemoveAll(x => x.StartsWith("U") || string.IsNullOrWhiteSpace(x));
                channelIds = channelIds.Distinct().ToList();

                await discordAPI.DeleteMessagesFromMultipleChannels(channelIds.Select(x => new DiscordAPI.DiscordMessageChannel
                {
                    isGuild = x.StartsWith("G", true, null),
                    channelID = x.ToUpper().Replace("G", "")
                }).ToArray(), (int totalChannelCount, int searchedChannelCount, int foundMessageCount, int deletedMessageCount) =>
                {
                    this.Invoke(new Action(() =>
                    {
                        foundMessagesLabel.Text = $"{deletedMessageCount} / {foundMessageCount}";
                        searchedChannelsLabel.Text = $"{searchedChannelCount} / {totalChannelCount}";

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
                    }));
                },
                (int rateLimitSeconds) =>
                {
                    toolStripStatusText.Text = $"Ratelimited for {rateLimitSeconds}s!";
                }, cts.Token);

                toolStripStatusText.Text = "Finished!";
            }
            catch (TaskCanceledException)
            {
                MessageBox.Show("Operation cancelled.");
                toolStripStatusText.Text = "Cancelled!";
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Encountered an error!");
                toolStripStatusText.Text = "Errored!";
            }
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
            MessageBox.Show("If there are multiple channel IDs, you can seperate them by using \",\"(comma)." + "\n" +
                "Add \"G\" in front of guild IDs." + "\n" +
                "Add \"U\" in front of user IDs." + "\n" +
                "NUKE button deletes all messages from DMs or guilds according to the checkboxes selected." + "\n" +
                "P.S. Guild means Discord server.");

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
            MessageBox.Show("Made by https://github.com/SnakePin." + Environment.NewLine + "Version: " + ProductVersion);
            //TODO: show about form here
        }
    }
}
