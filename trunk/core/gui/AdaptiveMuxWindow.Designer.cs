namespace MeGUI
{
    partial class AdaptiveMuxWindow
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
            this.containerFormat = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.helpButton1 = new MeGUI.core.gui.HelpButton();
            this.videoGroupbox.SuspendLayout();
            this.outputGroupbox.SuspendLayout();
            this.audioGroupbox.SuspendLayout();
            this.subtitleGroupbox.SuspendLayout();
            this.chaptersGroupbox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.audioDelay)).BeginInit();
            this.SuspendLayout();
            // 
            // muxButton
            // 
            this.muxButton.Location = new System.Drawing.Point(294, 461);
            // 
            // outputButton
            // 
            this.outputButton.Location = new System.Drawing.Point(380, 47);
            // 
            // muxedOutput
            // 
            this.muxedOutput.Location = new System.Drawing.Point(120, 47);
            // 
            // muxedOutputLabel
            // 
            this.muxedOutputLabel.Location = new System.Drawing.Point(14, 50);
            // 
            // enableSplit
            // 
            this.enableSplit.Location = new System.Drawing.Point(14, 76);
            // 
            // splitSize
            // 
            this.splitSize.Location = new System.Drawing.Point(120, 78);
            // 
            // mbLabel
            // 
            this.mbLabel.Location = new System.Drawing.Point(182, 80);
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(376, 461);
            // 
            // outputGroupbox
            // 
            this.outputGroupbox.Controls.Add(this.containerFormat);
            this.outputGroupbox.Controls.Add(this.label1);
            this.outputGroupbox.Size = new System.Drawing.Size(424, 111);
            this.outputGroupbox.Controls.SetChildIndex(this.mbLabel, 0);
            this.outputGroupbox.Controls.SetChildIndex(this.splitSize, 0);
            this.outputGroupbox.Controls.SetChildIndex(this.enableSplit, 0);
            this.outputGroupbox.Controls.SetChildIndex(this.muxedOutputLabel, 0);
            this.outputGroupbox.Controls.SetChildIndex(this.outputButton, 0);
            this.outputGroupbox.Controls.SetChildIndex(this.muxedOutput, 0);
            this.outputGroupbox.Controls.SetChildIndex(this.label1, 0);
            this.outputGroupbox.Controls.SetChildIndex(this.containerFormat, 0);
            // 
            // containerFormat
            // 
            this.containerFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.containerFormat.FormattingEnabled = true;
            this.containerFormat.Location = new System.Drawing.Point(120, 20);
            this.containerFormat.Name = "containerFormat";
            this.containerFormat.Size = new System.Drawing.Size(141, 21);
            this.containerFormat.TabIndex = 27;
            this.containerFormat.SelectedIndexChanged += new System.EventHandler(this.containerFormat_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(91, 13);
            this.label1.TabIndex = 25;
            this.label1.Text = "Container Format";
            // 
            // helpButton1
            // 
            this.helpButton1.ArticleName = "Adaptive mux window";
            this.helpButton1.AutoSize = true;
            this.helpButton1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.helpButton1.Location = new System.Drawing.Point(8, 461);
            this.helpButton1.Name = "helpButton1";
            this.helpButton1.Size = new System.Drawing.Size(38, 23);
            this.helpButton1.TabIndex = 30;
            // 
            // AdaptiveMuxWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(444, 490);
            this.Controls.Add(this.helpButton1);
            this.Name = "AdaptiveMuxWindow";
            this.Text = "Adaptive Mux Window";
            this.Controls.SetChildIndex(this.helpButton1, 0);
            this.Controls.SetChildIndex(this.chaptersGroupbox, 0);
            this.Controls.SetChildIndex(this.videoGroupbox, 0);
            this.Controls.SetChildIndex(this.audioGroupbox, 0);
            this.Controls.SetChildIndex(this.subtitleGroupbox, 0);
            this.Controls.SetChildIndex(this.muxButton, 0);
            this.Controls.SetChildIndex(this.outputGroupbox, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.videoGroupbox.ResumeLayout(false);
            this.videoGroupbox.PerformLayout();
            this.outputGroupbox.ResumeLayout(false);
            this.outputGroupbox.PerformLayout();
            this.audioGroupbox.ResumeLayout(false);
            this.audioGroupbox.PerformLayout();
            this.subtitleGroupbox.ResumeLayout(false);
            this.subtitleGroupbox.PerformLayout();
            this.chaptersGroupbox.ResumeLayout(false);
            this.chaptersGroupbox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.audioDelay)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox containerFormat;
        private System.Windows.Forms.Label label1;
        private MeGUI.core.gui.HelpButton helpButton1;
    }
}