using DiscordMessageDeleter.API;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordMessageDeleter
{
    public sealed partial class MainWindow : Window
    {
        private readonly DiscordAPI discordAPI = new();
        private CancellationTokenSource? cts;

        public MainWindow()
        {
            this.InitializeComponent();
        }

        private async Task handle_startButton_Click()
        {
            try
            {
                cts?.Cancel();
                cts?.Dispose();
                cts = new CancellationTokenSource();

                searchedChannelsLabel.Text = "N/A";
                foundMessagesLabel.Text = "N/A";
                toolStripProgressBar.Value = 0;

                bool eraseAllDMs = erase_DMS_CheckBox.IsChecked == true;
                bool eraseAllGuilds = erase_GUILDS_CheckBox.IsChecked == true;

                if (eraseAllDMs || eraseAllGuilds)
                {
                    var targets = new List<string>();
                    if (eraseAllDMs) targets.Add("DM(s)");
                    if (eraseAllGuilds) targets.Add("guild(s)");
                    string targetString = string.Join(" and ", targets);

                    var dialog = new ContentDialog
                    {
                        Title = "Warning",
                        Content = $"This operation will delete all messages in {targetString}. Are you sure you want to continue?",
                        PrimaryButtonText = "Yes",
                        CloseButtonText = "No",
                        XamlRoot = this.Content.XamlRoot
                    };

                    if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                    {
                        toolStripStatusText.Text = "Cancelled!";
                        return;
                    }
                }

                string authID = authID_TextBox.Text.Trim().Replace("\"", "");
                if (string.IsNullOrWhiteSpace(authID))
                {
                    throw new InvalidOperationException("Please fill the Auth ID text box!");
                }

                var progress = new Progress<DeleteProgress>(state =>
                {
                    foundMessagesLabel.Text = $"{state.DeletedMessageCount} / {state.FoundMessageCount}";
                    searchedChannelsLabel.Text = $"{state.SearchedChannelCount} / {state.TotalChannelCount}";

                    if (state.FoundMessageCount > 0)
                    {
                        toolStripProgressBar.Value = ((double)state.DeletedMessageCount / state.FoundMessageCount) * toolStripProgressBar.Maximum;
                    }

                    if (!string.IsNullOrWhiteSpace(state.StatusMessage))
                    {
                        toolStripStatusText.Text = state.StatusMessage;
                    }
                });

                await discordAPI.ExecuteDeleteOperationAsync(authID, channelIDsRTBox.Text, eraseAllDMs, eraseAllGuilds, progress, cts.Token);

                toolStripStatusText.Text = "Finished!";
            }
            catch (OperationCanceledException)
            {
                await ShowDialogAsync("Operation cancelled.");
                toolStripStatusText.Text = "Cancelled!";
            }
            catch (Exception exc)
            {
                await ShowDialogAsync(exc.Message, "Encountered an error!");
                toolStripStatusText.Text = "Errored!";
            }
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();
        }

        private async void startButton_Click(object sender, RoutedEventArgs e)
        {
            SetUiState(isEnabled: false);
            await handle_startButton_Click();
            SetUiState(isEnabled: true);
        }

        private async void helpButton_Click(object sender, RoutedEventArgs e) =>
            await ShowDialogAsync("If there are multiple channel IDs, you can separate them using a comma (\",\").\n" +
                "The application will automatically determine if an ID is a guild or a channel.\n");

        private async void aboutButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowDialogAsync("Version: GIT");
        }

        private void SetUiState(bool isEnabled)
        {
            authID_TextBox.IsEnabled = isEnabled;
            startButton.IsEnabled = isEnabled;
            erase_DMS_CheckBox.IsEnabled = isEnabled;
            erase_GUILDS_CheckBox.IsEnabled = isEnabled;
            channelIDsRTBox.IsEnabled = isEnabled;
        }

        private async Task ShowDialogAsync(string content, string title = "Information")
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
