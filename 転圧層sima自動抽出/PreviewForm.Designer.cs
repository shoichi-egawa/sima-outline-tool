namespace いきなりSIMAと外周線_ver2._0
{
    partial class PreviewForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true。破棄しない場合は false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            lblStatus = new Label();
            panelBottom = new Panel();
            btnResetView = new Button();
            btnConfirm = new Button();
            picPreview = new PictureBox();
            panelBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picPreview).BeginInit();
            SuspendLayout();
            // 
            // lblStatus
            // 
            lblStatus.BackColor = SystemColors.GradientInactiveCaption;
            lblStatus.Dock = DockStyle.Top;
            lblStatus.Font = new Font("メイリオ", 10F, FontStyle.Bold);
            lblStatus.ForeColor = Color.DarkBlue;
            lblStatus.Location = new Point(0, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Padding = new Padding(10, 0, 0, 0);
            lblStatus.Size = new Size(950, 30);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "【操作】　　左クリック：起点となる場所を指定（各層の最寄り頂点が起点となります） 　　ホイール：拡大縮小移動";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panelBottom
            // 
            panelBottom.BackColor = SystemColors.GradientActiveCaption;
            panelBottom.Controls.Add(btnResetView);
            panelBottom.Controls.Add(btnConfirm);
            panelBottom.Dock = DockStyle.Bottom;
            panelBottom.Location = new Point(0, 626);
            panelBottom.Name = "panelBottom";
            panelBottom.Size = new Size(950, 45);
            panelBottom.TabIndex = 1;
            // 
            // btnResetView
            // 
            btnResetView.Location = new Point(12, 8);
            btnResetView.Name = "btnResetView";
            btnResetView.Size = new Size(90, 30);
            btnResetView.TabIndex = 0;
            btnResetView.Text = "全体表示";
            btnResetView.UseVisualStyleBackColor = true;
            // 
            // btnConfirm
            // 
            btnConfirm.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnConfirm.DialogResult = DialogResult.OK;
            btnConfirm.Location = new Point(778, 8);
            btnConfirm.Name = "btnConfirm";
            btnConfirm.Size = new Size(160, 30);
            btnConfirm.TabIndex = 1;
            btnConfirm.Text = "起点を確定して閉じる";
            btnConfirm.UseVisualStyleBackColor = true;
            // 
            // picPreview
            // 
            picPreview.BackColor = Color.Black;
            picPreview.Dock = DockStyle.Fill;
            picPreview.Location = new Point(0, 30);
            picPreview.Name = "picPreview";
            picPreview.Size = new Size(950, 596);
            picPreview.TabIndex = 2;
            picPreview.TabStop = false;
            // 
            // PreviewForm
            // 
            ClientSize = new Size(950, 671);
            Controls.Add(picPreview);
            Controls.Add(panelBottom);
            Controls.Add(lblStatus);
            MinimumSize = new Size(500, 300);
            Name = "PreviewForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "図面プレビュー　起点位置指定";
            panelBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picPreview).EndInit();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Button btnResetView;
        private System.Windows.Forms.Button btnConfirm;
        private System.Windows.Forms.PictureBox picPreview;
    }
}