namespace ScheduledMailSender
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.tbxPwd = new System.Windows.Forms.TextBox();
            this.btnGo = new System.Windows.Forms.Button();
            this.cbxImapUri = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.cbxSmtpUri = new System.Windows.Forms.ComboBox();
            this.cbxUser = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(37, 42);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "User";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(37, 85);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Password";
            // 
            // tbxPwd
            // 
            this.tbxPwd.Location = new System.Drawing.Point(103, 82);
            this.tbxPwd.Name = "tbxPwd";
            this.tbxPwd.PasswordChar = '*';
            this.tbxPwd.Size = new System.Drawing.Size(257, 20);
            this.tbxPwd.TabIndex = 3;
            // 
            // btnGo
            // 
            this.btnGo.Location = new System.Drawing.Point(428, 42);
            this.btnGo.Name = "btnGo";
            this.btnGo.Size = new System.Drawing.Size(96, 23);
            this.btnGo.TabIndex = 4;
            this.btnGo.Text = "Go";
            this.btnGo.UseVisualStyleBackColor = true;
            this.btnGo.Click += new System.EventHandler(this.btnGo_Click);
            // 
            // cbxImapUri
            // 
            this.cbxImapUri.FormattingEnabled = true;
            this.cbxImapUri.Items.AddRange(new object[] {
            "imaps://imap-mail.outlook.com",
            "imaps://imap.gmail.com"});
            this.cbxImapUri.Location = new System.Drawing.Point(103, 125);
            this.cbxImapUri.Name = "cbxImapUri";
            this.cbxImapUri.Size = new System.Drawing.Size(257, 21);
            this.cbxImapUri.TabIndex = 7;
            this.cbxImapUri.Text = "imaps://imap-mail.outlook.com";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(40, 128);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(55, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "IMAP URI";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(40, 173);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(59, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "SMTP URI";
            // 
            // cbxSmtpUri
            // 
            this.cbxSmtpUri.FormattingEnabled = true;
            this.cbxSmtpUri.Items.AddRange(new object[] {
            "smtp://smtp-mail.outlook.com:587",
            "smtp://smtp.gmail.com:587"});
            this.cbxSmtpUri.Location = new System.Drawing.Point(103, 170);
            this.cbxSmtpUri.Name = "cbxSmtpUri";
            this.cbxSmtpUri.Size = new System.Drawing.Size(257, 21);
            this.cbxSmtpUri.TabIndex = 10;
            this.cbxSmtpUri.Text = "smtp://smtp-mail.outlook.com:587";
            // 
            // cbxUser
            // 
            this.cbxUser.FormattingEnabled = true;
            this.cbxUser.Items.AddRange(new object[] {
            "wuyijun1129@hotmail.com",
            "wuyijun1129@gmail.com"});
            this.cbxUser.Location = new System.Drawing.Point(103, 34);
            this.cbxUser.Name = "cbxUser";
            this.cbxUser.Size = new System.Drawing.Size(257, 21);
            this.cbxUser.TabIndex = 11;
            this.cbxUser.Text = "wuyijun1129@hotmail.com";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(638, 385);
            this.Controls.Add(this.cbxUser);
            this.Controls.Add(this.cbxSmtpUri);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cbxImapUri);
            this.Controls.Add(this.btnGo);
            this.Controls.Add(this.tbxPwd);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Check Drafts And Send";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbxPwd;
        private System.Windows.Forms.Button btnGo;
        private System.Windows.Forms.ComboBox cbxImapUri;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cbxSmtpUri;
        private System.Windows.Forms.ComboBox cbxUser;
    }
}

