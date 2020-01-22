namespace MaviSoftServerV1._0
{
    partial class FrmActivation
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmActivation));
            this.txtActivationKey = new System.Windows.Forms.TextBox();
            this.txtActivationCode = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnYenile = new System.Windows.Forms.Button();
            this.btnKontrol = new System.Windows.Forms.Button();
            this.btnKaydet = new System.Windows.Forms.Button();
            this.btnKapat = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.chkBoxActivation = new System.Windows.Forms.CheckBox();
            this.lblActivationStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtActivationKey
            // 
            this.txtActivationKey.Location = new System.Drawing.Point(12, 41);
            this.txtActivationKey.Multiline = true;
            this.txtActivationKey.Name = "txtActivationKey";
            this.txtActivationKey.Size = new System.Drawing.Size(328, 21);
            this.txtActivationKey.TabIndex = 0;
            // 
            // txtActivationCode
            // 
            this.txtActivationCode.Enabled = false;
            this.txtActivationCode.Location = new System.Drawing.Point(12, 94);
            this.txtActivationCode.Multiline = true;
            this.txtActivationCode.Name = "txtActivationCode";
            this.txtActivationCode.Size = new System.Drawing.Size(328, 21);
            this.txtActivationCode.TabIndex = 1;
            this.txtActivationCode.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label1.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.label1.Location = new System.Drawing.Point(12, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(133, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "Aktivasyon Anahtarı";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label2.Location = new System.Drawing.Point(12, 74);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(113, 17);
            this.label2.TabIndex = 3;
            this.label2.Text = "Aktivasyon Kodu";
            // 
            // btnYenile
            // 
            this.btnYenile.Location = new System.Drawing.Point(346, 41);
            this.btnYenile.Name = "btnYenile";
            this.btnYenile.Size = new System.Drawing.Size(75, 23);
            this.btnYenile.TabIndex = 4;
            this.btnYenile.Text = "Yenile";
            this.btnYenile.UseVisualStyleBackColor = true;
            this.btnYenile.Click += new System.EventHandler(this.btnYenile_Click);
            // 
            // btnKontrol
            // 
            this.btnKontrol.Location = new System.Drawing.Point(346, 92);
            this.btnKontrol.Name = "btnKontrol";
            this.btnKontrol.Size = new System.Drawing.Size(75, 23);
            this.btnKontrol.TabIndex = 5;
            this.btnKontrol.Text = "Kontrol";
            this.btnKontrol.UseVisualStyleBackColor = true;
            this.btnKontrol.Click += new System.EventHandler(this.btnKontrol_Click);
            // 
            // btnKaydet
            // 
            this.btnKaydet.Location = new System.Drawing.Point(11, 178);
            this.btnKaydet.Name = "btnKaydet";
            this.btnKaydet.Size = new System.Drawing.Size(197, 31);
            this.btnKaydet.TabIndex = 6;
            this.btnKaydet.Text = "Aktivasyon Kodunu Kaydet";
            this.btnKaydet.UseVisualStyleBackColor = true;
            this.btnKaydet.Click += new System.EventHandler(this.btnKaydet_Click);
            // 
            // btnKapat
            // 
            this.btnKapat.Location = new System.Drawing.Point(223, 178);
            this.btnKapat.Name = "btnKapat";
            this.btnKapat.Size = new System.Drawing.Size(198, 31);
            this.btnKapat.TabIndex = 7;
            this.btnKapat.Text = "Pencereyi Kapat";
            this.btnKapat.UseVisualStyleBackColor = true;
            this.btnKapat.Click += new System.EventHandler(this.btnKapat_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 122);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(136, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Yeni Aktivasyon Kodu Girişi";
            // 
            // chkBoxActivation
            // 
            this.chkBoxActivation.AutoSize = true;
            this.chkBoxActivation.Location = new System.Drawing.Point(154, 121);
            this.chkBoxActivation.Name = "chkBoxActivation";
            this.chkBoxActivation.Size = new System.Drawing.Size(15, 14);
            this.chkBoxActivation.TabIndex = 9;
            this.chkBoxActivation.UseVisualStyleBackColor = true;
            this.chkBoxActivation.CheckedChanged += new System.EventHandler(this.chkBoxActivation_CheckedChanged);
            // 
            // lblActivationStatus
            // 
            this.lblActivationStatus.AutoSize = true;
            this.lblActivationStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.lblActivationStatus.Location = new System.Drawing.Point(24, 144);
            this.lblActivationStatus.Name = "lblActivationStatus";
            this.lblActivationStatus.Size = new System.Drawing.Size(0, 17);
            this.lblActivationStatus.TabIndex = 10;
            this.lblActivationStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // FrmActivation
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(432, 221);
            this.Controls.Add(this.lblActivationStatus);
            this.Controls.Add(this.chkBoxActivation);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnKapat);
            this.Controls.Add(this.btnKaydet);
            this.Controls.Add(this.btnKontrol);
            this.Controls.Add(this.btnYenile);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtActivationCode);
            this.Controls.Add(this.txtActivationKey);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmActivation";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Ürün Etkinleştirme";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmActivation_FormClosing);
            this.Load += new System.EventHandler(this.FrmActivation_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtActivationKey;
        private System.Windows.Forms.TextBox txtActivationCode;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnYenile;
        private System.Windows.Forms.Button btnKontrol;
        private System.Windows.Forms.Button btnKaydet;
        private System.Windows.Forms.Button btnKapat;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox chkBoxActivation;
        private System.Windows.Forms.Label lblActivationStatus;
    }
}