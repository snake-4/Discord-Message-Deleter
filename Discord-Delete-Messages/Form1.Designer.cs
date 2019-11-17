namespace Discord_Delete_Messages
{
    partial class Form1
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
            this.authID_TextBox = new System.Windows.Forms.TextBox();
            this.startButton = new System.Windows.Forms.Button();
            this.outputRTBox = new System.Windows.Forms.RichTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.channelIDsRTBox = new System.Windows.Forms.RichTextBox();
            this.helpButton = new System.Windows.Forms.Button();
            this.nukeButton = new System.Windows.Forms.Button();
            this.nuke_GUILDS_CheckBox = new System.Windows.Forms.CheckBox();
            this.nuke_DMS_CheckBox = new System.Windows.Forms.CheckBox();
            this.aboutButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // authID_TextBox
            // 
            this.authID_TextBox.Location = new System.Drawing.Point(67, 12);
            this.authID_TextBox.Name = "authID_TextBox";
            this.authID_TextBox.Size = new System.Drawing.Size(393, 20);
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
            // outputRTBox
            // 
            this.outputRTBox.Location = new System.Drawing.Point(12, 223);
            this.outputRTBox.Name = "outputRTBox";
            this.outputRTBox.Size = new System.Drawing.Size(448, 53);
            this.outputRTBox.TabIndex = 3;
            this.outputRTBox.Text = "";
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
            this.label2.Location = new System.Drawing.Point(12, 15);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Auth ID :";
            // 
            // channelIDsRTBox
            // 
            this.channelIDsRTBox.Location = new System.Drawing.Point(12, 55);
            this.channelIDsRTBox.Name = "channelIDsRTBox";
            this.channelIDsRTBox.Size = new System.Drawing.Size(339, 162);
            this.channelIDsRTBox.TabIndex = 6;
            this.channelIDsRTBox.Text = "";
            // 
            // helpButton
            // 
            this.helpButton.Location = new System.Drawing.Point(357, 165);
            this.helpButton.Name = "helpButton";
            this.helpButton.Size = new System.Drawing.Size(103, 23);
            this.helpButton.TabIndex = 7;
            this.helpButton.Text = "Help";
            this.helpButton.UseVisualStyleBackColor = true;
            this.helpButton.Click += new System.EventHandler(this.helpButton_Click);
            // 
            // nukeButton
            // 
            this.nukeButton.Location = new System.Drawing.Point(357, 84);
            this.nukeButton.Name = "nukeButton";
            this.nukeButton.Size = new System.Drawing.Size(103, 23);
            this.nukeButton.TabIndex = 8;
            this.nukeButton.Text = "NUKE";
            this.nukeButton.UseVisualStyleBackColor = true;
            this.nukeButton.Click += new System.EventHandler(this.nukeButton_Click);
            // 
            // nuke_GUILDS_CheckBox
            // 
            this.nuke_GUILDS_CheckBox.AutoSize = true;
            this.nuke_GUILDS_CheckBox.Location = new System.Drawing.Point(357, 136);
            this.nuke_GUILDS_CheckBox.Name = "nuke_GUILDS_CheckBox";
            this.nuke_GUILDS_CheckBox.Size = new System.Drawing.Size(103, 17);
            this.nuke_GUILDS_CheckBox.TabIndex = 9;
            this.nuke_GUILDS_CheckBox.Text = "NUKE GUILD(s)";
            this.nuke_GUILDS_CheckBox.UseVisualStyleBackColor = true;
            // 
            // nuke_DMS_CheckBox
            // 
            this.nuke_DMS_CheckBox.AutoSize = true;
            this.nuke_DMS_CheckBox.Location = new System.Drawing.Point(357, 113);
            this.nuke_DMS_CheckBox.Name = "nuke_DMS_CheckBox";
            this.nuke_DMS_CheckBox.Size = new System.Drawing.Size(87, 17);
            this.nuke_DMS_CheckBox.TabIndex = 10;
            this.nuke_DMS_CheckBox.Text = "NUKE DM(s)";
            this.nuke_DMS_CheckBox.UseVisualStyleBackColor = true;
            // 
            // aboutButton
            // 
            this.aboutButton.Location = new System.Drawing.Point(357, 194);
            this.aboutButton.Name = "aboutButton";
            this.aboutButton.Size = new System.Drawing.Size(103, 23);
            this.aboutButton.TabIndex = 11;
            this.aboutButton.Text = "About";
            this.aboutButton.UseVisualStyleBackColor = true;
            this.aboutButton.Click += new System.EventHandler(this.aboutButton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(472, 286);
            this.Controls.Add(this.aboutButton);
            this.Controls.Add(this.nuke_DMS_CheckBox);
            this.Controls.Add(this.nuke_GUILDS_CheckBox);
            this.Controls.Add(this.nukeButton);
            this.Controls.Add(this.helpButton);
            this.Controls.Add(this.channelIDsRTBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.outputRTBox);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.authID_TextBox);
            this.Name = "Form1";
            this.Text = "Discord Message Deleter by SnakePin v1.5";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox authID_TextBox;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.RichTextBox outputRTBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RichTextBox channelIDsRTBox;
        private System.Windows.Forms.Button helpButton;
        private System.Windows.Forms.Button nukeButton;
        private System.Windows.Forms.CheckBox nuke_GUILDS_CheckBox;
        private System.Windows.Forms.CheckBox nuke_DMS_CheckBox;
        private System.Windows.Forms.Button aboutButton;
    }
}

