using System.Windows.Forms;

namespace いきなりSIMAと外周線_ver2._0
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        private void InitializeComponent()
        {
            txtKoujimei = new TextBox();
            chkLayerList = new CheckedListBox();
            lblFilePath = new Label();
            btnSelectFile = new Button();
            btnSelectAll = new Button();
            btnDeselectAll = new Button();
            btnRun = new Button();
            chkSimplify = new CheckBox();
            numTolerance = new NumericUpDown();
            lblToleranceUnit = new Label();
            chkKeepZ = new CheckBox();
            chkBridgeIslands = new CheckBox();
            ((System.ComponentModel.ISupportInitialize)numTolerance).BeginInit();
            SuspendLayout();
            // 
            // txtKoujimei
            // 
            txtKoujimei.Location = new Point(12, 12);
            txtKoujimei.Name = "txtKoujimei";
            txtKoujimei.Size = new Size(265, 23);
            txtKoujimei.TabIndex = 0;
            // 
            // chkLayerList
            // 
            chkLayerList.FormattingEnabled = true;
            chkLayerList.Location = new Point(12, 182);
            chkLayerList.Name = "chkLayerList";
            chkLayerList.Size = new Size(265, 562);
            chkLayerList.TabIndex = 3;
            // 
            // lblFilePath
            // 
            lblFilePath.AutoSize = true;
            lblFilePath.Location = new Point(172, 48);
            lblFilePath.Name = "lblFilePath";
            lblFilePath.Size = new Size(77, 15);
            lblFilePath.TabIndex = 2;
            lblFilePath.Text = "ファイル未選択";
            lblFilePath.Click += lblFilePath_Click;
            // 
            // btnSelectFile
            // 
            btnSelectFile.Location = new Point(12, 41);
            btnSelectFile.Name = "btnSelectFile";
            btnSelectFile.Size = new Size(151, 28);
            btnSelectFile.TabIndex = 1;
            btnSelectFile.Text = "dxf / landxml ファイル選択";
            btnSelectFile.UseVisualStyleBackColor = true;
            btnSelectFile.Click += btnSelectFile_Click;
            // 
            // btnSelectAll
            // 
            btnSelectAll.Location = new Point(12, 150);
            btnSelectAll.Name = "btnSelectAll";
            btnSelectAll.Size = new Size(66, 26);
            btnSelectAll.TabIndex = 4;
            btnSelectAll.Text = "全選択";
            btnSelectAll.UseVisualStyleBackColor = true;
            btnSelectAll.Click += btnSelectAll_Click;
            // 
            // btnDeselectAll
            // 
            btnDeselectAll.Location = new Point(84, 150);
            btnDeselectAll.Name = "btnDeselectAll";
            btnDeselectAll.Size = new Size(66, 26);
            btnDeselectAll.TabIndex = 5;
            btnDeselectAll.Text = "全解除";
            btnDeselectAll.UseVisualStyleBackColor = true;
            btnDeselectAll.Click += btnDeselectAll_Click;
            // 
            // btnRun
            // 
            btnRun.Font = new Font("メイリオ", 9F, FontStyle.Regular, GraphicsUnit.Point, 128);
            btnRun.Location = new Point(172, 750);
            btnRun.Name = "btnRun";
            btnRun.Size = new Size(105, 29);
            btnRun.TabIndex = 7;
            btnRun.Text = "SIMA出力実行";
            btnRun.UseVisualStyleBackColor = true;
            btnRun.Click += btnRun_Click;
            // 
            // chkSimplify
            // 
            chkSimplify.AutoSize = true;
            chkSimplify.Location = new Point(12, 101);
            chkSimplify.Name = "chkSimplify";
            chkSimplify.Size = new Size(97, 19);
            chkSimplify.TabIndex = 8;
            chkSimplify.Text = "折れ点 間引き";
            chkSimplify.UseVisualStyleBackColor = true;
            chkSimplify.CheckedChanged += chkSimplify_CheckedChanged;
            // 
            // numTolerance
            // 
            numTolerance.DecimalPlaces = 1;
            numTolerance.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            numTolerance.Location = new Point(115, 97);
            numTolerance.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
            numTolerance.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            numTolerance.Name = "numTolerance";
            numTolerance.Size = new Size(48, 23);
            numTolerance.TabIndex = 9;
            numTolerance.Value = new decimal(new int[] { 20, 0, 0, 65536 });
            numTolerance.ValueChanged += numTolerance_ValueChanged;
            // 
            // lblToleranceUnit
            // 
            lblToleranceUnit.AutoSize = true;
            lblToleranceUnit.Location = new Point(169, 102);
            lblToleranceUnit.Name = "lblToleranceUnit";
            lblToleranceUnit.Size = new Size(98, 15);
            lblToleranceUnit.TabIndex = 10;
            lblToleranceUnit.Text = "cm離れを許容する";
            lblToleranceUnit.Click += lblToleranceUnit_Click;
            // 
            // chkKeepZ
            // 
            chkKeepZ.AutoSize = true;
            chkKeepZ.Location = new Point(12, 75);
            chkKeepZ.Name = "chkKeepZ";
            chkKeepZ.Size = new Size(129, 19);
            chkKeepZ.TabIndex = 6;
            chkKeepZ.Text = "標高(Z値)も出力する";
            chkKeepZ.UseVisualStyleBackColor = true;
            chkKeepZ.CheckedChanged += checkBox1_CheckedChanged;
            // 
            // chkBridgeIslands
            // 
            chkBridgeIslands.AutoSize = true;
            chkBridgeIslands.Location = new Point(12, 125);
            chkBridgeIslands.Name = "chkBridgeIslands";
            chkBridgeIslands.Size = new Size(257, 19);
            chkBridgeIslands.TabIndex = 11;
            chkBridgeIslands.Text = "同一層の複数小島を10cm幅ブリッジで結合する";
            chkBridgeIslands.UseVisualStyleBackColor = true;
            chkBridgeIslands.CheckedChanged += chkBridgeIslands_CheckedChanged;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.GradientInactiveCaption;
            ClientSize = new Size(296, 802);
            Controls.Add(chkBridgeIslands);
            Controls.Add(lblToleranceUnit);
            Controls.Add(numTolerance);
            Controls.Add(chkSimplify);
            Controls.Add(btnRun);
            Controls.Add(chkKeepZ);
            Controls.Add(btnDeselectAll);
            Controls.Add(btnSelectAll);
            Controls.Add(chkLayerList);
            Controls.Add(lblFilePath);
            Controls.Add(btnSelectFile);
            Controls.Add(txtKoujimei);
            Name = "Form1";
            Text = "いきなり SIMAと外周線";
            ((System.ComponentModel.ISupportInitialize)numTolerance).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox txtKoujimei;
        private System.Windows.Forms.CheckedListBox chkLayerList;
        private System.Windows.Forms.Label lblFilePath;
        private System.Windows.Forms.Button btnSelectFile;
        private System.Windows.Forms.Button btnSelectAll;
        private System.Windows.Forms.Button btnDeselectAll;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.CheckBox chkSimplify;
        private System.Windows.Forms.NumericUpDown numTolerance;
        private System.Windows.Forms.Label lblToleranceUnit;
        private CheckBox chkKeepZ;
        private CheckBox chkBridgeIslands;
    }
}