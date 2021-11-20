namespace DiscordMessageDeleter
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.authID_TextBox = new System.Windows.Forms.TextBox();
            this.startButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.channelIDsRTBox = new System.Windows.Forms.RichTextBox();
            this.helpButton = new System.Windows.Forms.Button();
            this.stopButton = new System.Windows.Forms.Button();
            this.nuke_GUILDS_CheckBox = new System.Windows.Forms.CheckBox();
            this.nuke_DMS_CheckBox = new System.Windows.Forms.CheckBox();
            this.aboutButton = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusText = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel3 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.searchedChannelsLabel = new System.Windows.Forms.Label();
            this.foundMessagesLabel = new System.Windows.Forms.Label();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // authID_TextBox
            // 
            this.authID_TextBox.Location = new System.Drawing.Point(61, 12);
            this.authID_TextBox.Name = "authID_TextBox";
            this.authID_TextBox.Size = new System.Drawing.Size(397, 20);
            this.authID_TextBox.TabIndex = 0;
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(357, 55);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(103, 23);
            this.startButton.TabIndex = 2;
            this.startButton.Text = "Start";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Channel ID(s) :";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 15);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Auth ID:";
            // 
            // channelIDsRTBox
            // 
            this.channelIDsRTBox.Location = new System.Drawing.Point(12, 55);
            this.channelIDsRTBox.Name = "channelIDsRTBox";
            this.channelIDsRTBox.Size = new System.Drawing.Size(339, 156);
            this.channelIDsRTBox.TabIndex = 6;
            this.channelIDsRTBox.Text = "";
            // 
            // helpButton
            // 
            this.helpButton.Location = new System.Drawing.Point(357, 159);
            this.helpButton.Name = "helpButton";
            this.helpButton.Size = new System.Drawing.Size(103, 23);
            this.helpButton.TabIndex = 7;
            this.helpButton.Text = "Help";
            this.helpButton.UseVisualStyleBackColor = true;
            this.helpButton.Click += new System.EventHandler(this.helpButton_Click);
            // 
            // stopButton
            // 
            this.stopButton.Location = new System.Drawing.Point(357, 84);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(103, 23);
            this.stopButton.TabIndex = 8;
            this.stopButton.Text = "Stop";
            this.stopButton.UseVisualStyleBackColor = true;
            this.stopButton.Click += new System.EventHandler(this.stopButton_Click);
            // 
            // nuke_GUILDS_CheckBox
            // 
            this.nuke_GUILDS_CheckBox.AutoSize = true;
            this.nuke_GUILDS_CheckBox.Location = new System.Drawing.Point(357, 136);
            this.nuke_GUILDS_CheckBox.Name = "nuke_GUILDS_CheckBox";
            this.nuke_GUILDS_CheckBox.Size = new System.Drawing.Size(117, 17);
            this.nuke_GUILDS_CheckBox.TabIndex = 9;
            this.nuke_GUILDS_CheckBox.Text = "Delete all GUILD(s)";
            this.nuke_GUILDS_CheckBox.UseVisualStyleBackColor = true;
            // 
            // nuke_DMS_CheckBox
            // 
            this.nuke_DMS_CheckBox.AutoSize = true;
            this.nuke_DMS_CheckBox.Location = new System.Drawing.Point(357, 113);
            this.nuke_DMS_CheckBox.Name = "nuke_DMS_CheckBox";
            this.nuke_DMS_CheckBox.Size = new System.Drawing.Size(101, 17);
            this.nuke_DMS_CheckBox.TabIndex = 10;
            this.nuke_DMS_CheckBox.Text = "Delete all DM(s)";
            this.nuke_DMS_CheckBox.UseVisualStyleBackColor = true;
            // 
            // aboutButton
            // 
            this.aboutButton.Location = new System.Drawing.Point(357, 188);
            this.aboutButton.Name = "aboutButton";
            this.aboutButton.Size = new System.Drawing.Size(103, 23);
            this.aboutButton.TabIndex = 11;
            this.aboutButton.Text = "About";
            this.aboutButton.UseVisualStyleBackColor = true;
            this.aboutButton.Click += new System.EventHandler(this.aboutButton_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel2,
            this.toolStripStatusText,
            this.toolStripStatusLabel3,
            this.toolStripStatusLabel1,
            this.toolStripProgressBar});
            this.statusStrip1.Location = new System.Drawing.Point(0, 238);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(472, 22);
            this.statusStrip1.TabIndex = 12;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(42, 17);
            this.toolStripStatusLabel2.Text = "Status:";
            // 
            // toolStripStatusText
            // 
            this.toolStripStatusText.Name = "toolStripStatusText";
            this.toolStripStatusText.Size = new System.Drawing.Size(39, 17);
            this.toolStripStatusText.Text = "Ready";
            // 
            // toolStripStatusLabel3
            // 
            this.toolStripStatusLabel3.Name = "toolStripStatusLabel3";
            this.toolStripStatusLabel3.Size = new System.Drawing.Size(119, 17);
            this.toolStripStatusLabel3.Spring = true;
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(55, 17);
            this.toolStripStatusLabel1.Text = "Progress:";
            // 
            // toolStripProgressBar
            // 
            this.toolStripProgressBar.Maximum = 1000;
            this.toolStripProgressBar.Name = "toolStripProgressBar";
            this.toolStripProgressBar.Size = new System.Drawing.Size(200, 16);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 218);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(102, 13);
            this.label3.TabIndex = 13;
            this.label3.Text = "Searched channels:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(226, 218);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(90, 13);
            this.label4.TabIndex = 14;
            this.label4.Text = "Found messages:";
            // 
            // searchedChannelsLabel
            // 
            this.searchedChannelsLabel.AutoSize = true;
            this.searchedChannelsLabel.Location = new System.Drawing.Point(120, 218);
            this.searchedChannelsLabel.Name = "searchedChannelsLabel";
            this.searchedChannelsLabel.Size = new System.Drawing.Size(27, 13);
            this.searchedChannelsLabel.TabIndex = 15;
            this.searchedChannelsLabel.Text = "N/A";
            // 
            // foundMessagesLabel
            // 
            this.foundMessagesLabel.AutoSize = true;
            this.foundMessagesLabel.Location = new System.Drawing.Point(322, 218);
            this.foundMessagesLabel.Name = "foundMessagesLabel";
            this.foundMessagesLabel.Size = new System.Drawing.Size(27, 13);
            this.foundMessagesLabel.TabIndex = 16;
            this.foundMessagesLabel.Text = "N/A";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(472, 260);
            this.Controls.Add(this.foundMessagesLabel);
            this.Controls.Add(this.searchedChannelsLabel);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.aboutButton);
            this.Controls.Add(this.nuke_DMS_CheckBox);
            this.Controls.Add(this.nuke_GUILDS_CheckBox);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.helpButton);
            this.Controls.Add(this.channelIDsRTBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.authID_TextBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Discord Message Deleter";
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox authID_TextBox;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RichTextBox channelIDsRTBox;
        private System.Windows.Forms.Button helpButton;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.CheckBox nuke_GUILDS_CheckBox;
        private System.Windows.Forms.CheckBox nuke_DMS_CheckBox;
        private System.Windows.Forms.Button aboutButton;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusText;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label searchedChannelsLabel;
        private System.Windows.Forms.Label foundMessagesLabel;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel3;
    }
}

