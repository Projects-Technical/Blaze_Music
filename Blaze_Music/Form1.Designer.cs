namespace Blaze_Music
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.WMP = new AxWMPLib.AxWindowsMediaPlayer();
            this.skpTime = new System.Windows.Forms.NumericUpDown();
            this.btnSkip = new System.Windows.Forms.Button();
            this.lblCurTime = new System.Windows.Forms.Label();
            this.wmpTimer = new System.Windows.Forms.Timer(this.components);
            this.lblrcnt = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.WMP)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.skpTime)).BeginInit();
            this.SuspendLayout();
            // 
            // WMP
            // 
            this.WMP.Enabled = true;
            this.WMP.Location = new System.Drawing.Point(0, 3);
            this.WMP.Name = "WMP";
            this.WMP.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("WMP.OcxState")));
            this.WMP.Size = new System.Drawing.Size(398, 182);
            this.WMP.TabIndex = 0;
            this.WMP.Enter += new System.EventHandler(this.WMP_Enter);
            // 
            // skpTime
            // 
            this.skpTime.Enabled = false;
            this.skpTime.Location = new System.Drawing.Point(4, 186);
            this.skpTime.Name = "skpTime";
            this.skpTime.Size = new System.Drawing.Size(71, 20);
            this.skpTime.TabIndex = 1;
            // 
            // btnSkip
            // 
            this.btnSkip.Enabled = false;
            this.btnSkip.Location = new System.Drawing.Point(75, 185);
            this.btnSkip.Name = "btnSkip";
            this.btnSkip.Size = new System.Drawing.Size(97, 22);
            this.btnSkip.TabIndex = 2;
            this.btnSkip.Text = "Skip to Time (s)";
            this.btnSkip.UseVisualStyleBackColor = true;
            this.btnSkip.Click += new System.EventHandler(this.btnSkip_Click);
            // 
            // lblCurTime
            // 
            this.lblCurTime.AutoSize = true;
            this.lblCurTime.BackColor = System.Drawing.Color.Transparent;
            this.lblCurTime.ForeColor = System.Drawing.Color.Transparent;
            this.lblCurTime.Location = new System.Drawing.Point(178, 190);
            this.lblCurTime.Name = "lblCurTime";
            this.lblCurTime.Size = new System.Drawing.Size(34, 13);
            this.lblCurTime.TabIndex = 3;
            this.lblCurTime.Text = "00:00";
            // 
            // wmpTimer
            // 
            this.wmpTimer.Enabled = true;
            this.wmpTimer.Tick += new System.EventHandler(this.wmpTimer_Tick);
            // 
            // lblrcnt
            // 
            this.lblrcnt.AutoSize = true;
            this.lblrcnt.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.lblrcnt.Location = new System.Drawing.Point(332, 113);
            this.lblrcnt.Name = "lblrcnt";
            this.lblrcnt.Size = new System.Drawing.Size(0, 13);
            this.lblrcnt.TabIndex = 4;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(398, 231);
            this.Controls.Add(this.lblrcnt);
            this.Controls.Add(this.lblCurTime);
            this.Controls.Add(this.btnSkip);
            this.Controls.Add(this.skpTime);
            this.Controls.Add(this.WMP);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(414, 270);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(414, 270);
            this.Name = "Form1";
            this.Text = "Blaze Music Player";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.WMP)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.skpTime)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private AxWMPLib.AxWindowsMediaPlayer WMP;
        private System.Windows.Forms.NumericUpDown skpTime;
        private System.Windows.Forms.Button btnSkip;
        private System.Windows.Forms.Label lblCurTime;
        private System.Windows.Forms.Timer wmpTimer;
        private System.Windows.Forms.Label lblrcnt;
    }
}

