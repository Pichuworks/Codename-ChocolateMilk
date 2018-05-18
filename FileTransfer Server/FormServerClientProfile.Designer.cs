namespace FileTransfer_Server
{
    partial class FormServerClientProfile
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
            this.labelCPU = new System.Windows.Forms.Label();
            this.labelOS = new System.Windows.Forms.Label();
            this.labelMAC = new System.Windows.Forms.Label();
            this.labelUser = new System.Windows.Forms.Label();
            this.labelPCName = new System.Windows.Forms.Label();
            this.labelStdNo = new System.Windows.Forms.Label();
            this.labelStdName = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // labelCPU
            // 
            this.labelCPU.AutoSize = true;
            this.labelCPU.Location = new System.Drawing.Point(32, 106);
            this.labelCPU.Name = "labelCPU";
            this.labelCPU.Size = new System.Drawing.Size(47, 12);
            this.labelCPU.TabIndex = 11;
            this.labelCPU.Text = "处理器:";
            // 
            // labelOS
            // 
            this.labelOS.AutoSize = true;
            this.labelOS.Location = new System.Drawing.Point(32, 135);
            this.labelOS.Name = "labelOS";
            this.labelOS.Size = new System.Drawing.Size(59, 12);
            this.labelOS.TabIndex = 10;
            this.labelOS.Text = "操作系统:";
            // 
            // labelMAC
            // 
            this.labelMAC.AutoSize = true;
            this.labelMAC.Location = new System.Drawing.Point(32, 77);
            this.labelMAC.Name = "labelMAC";
            this.labelMAC.Size = new System.Drawing.Size(53, 12);
            this.labelMAC.TabIndex = 9;
            this.labelMAC.Text = "MAC地址:";
            // 
            // labelUser
            // 
            this.labelUser.AutoSize = true;
            this.labelUser.Location = new System.Drawing.Point(32, 48);
            this.labelUser.Name = "labelUser";
            this.labelUser.Size = new System.Drawing.Size(35, 12);
            this.labelUser.TabIndex = 8;
            this.labelUser.Text = "用户:";
            // 
            // labelPCName
            // 
            this.labelPCName.AutoSize = true;
            this.labelPCName.Location = new System.Drawing.Point(32, 19);
            this.labelPCName.Name = "labelPCName";
            this.labelPCName.Size = new System.Drawing.Size(59, 12);
            this.labelPCName.TabIndex = 7;
            this.labelPCName.Text = "计算机名:";
            // 
            // labelStdNo
            // 
            this.labelStdNo.AutoSize = true;
            this.labelStdNo.Location = new System.Drawing.Point(32, 164);
            this.labelStdNo.Name = "labelStdNo";
            this.labelStdNo.Size = new System.Drawing.Size(35, 12);
            this.labelStdNo.TabIndex = 12;
            this.labelStdNo.Text = "学号:";
            // 
            // labelStdName
            // 
            this.labelStdName.AutoSize = true;
            this.labelStdName.Location = new System.Drawing.Point(32, 193);
            this.labelStdName.Name = "labelStdName";
            this.labelStdName.Size = new System.Drawing.Size(35, 12);
            this.labelStdName.TabIndex = 13;
            this.labelStdName.Text = "姓名:";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(297, 226);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 14;
            this.button1.Text = "确认";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // FormServerClientProfile
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 261);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.labelStdName);
            this.Controls.Add(this.labelStdNo);
            this.Controls.Add(this.labelCPU);
            this.Controls.Add(this.labelOS);
            this.Controls.Add(this.labelMAC);
            this.Controls.Add(this.labelUser);
            this.Controls.Add(this.labelPCName);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormServerClientProfile";
            this.Text = "客户端信息 - ";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelCPU;
        private System.Windows.Forms.Label labelOS;
        private System.Windows.Forms.Label labelMAC;
        private System.Windows.Forms.Label labelUser;
        private System.Windows.Forms.Label labelPCName;
        private System.Windows.Forms.Label labelStdNo;
        private System.Windows.Forms.Label labelStdName;
        private System.Windows.Forms.Button button1;
    }
}