namespace Shap.ShapDoc.CodeConverter
{
    partial class MainWindow
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tableLayoutPanel1 = new TableLayoutPanel();
            rtbRezult = new RichTextBox();
            btnPaste = new Button();
            btnCopy = new Button();
            cbLang = new ComboBox();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            tableLayoutPanel1.Controls.Add(rtbRezult, 0, 0);
            tableLayoutPanel1.Controls.Add(btnPaste, 2, 1);
            tableLayoutPanel1.Controls.Add(btnCopy, 1, 1);
            tableLayoutPanel1.Controls.Add(cbLang, 0, 1);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableLayoutPanel1.Size = new Size(562, 321);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // rtbRezult
            // 
            rtbRezult.BorderStyle = BorderStyle.FixedSingle;
            tableLayoutPanel1.SetColumnSpan(rtbRezult, 3);
            rtbRezult.Dock = DockStyle.Fill;
            rtbRezult.Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rtbRezult.Location = new Point(3, 3);
            rtbRezult.Name = "rtbRezult";
            rtbRezult.Size = new Size(556, 285);
            rtbRezult.TabIndex = 0;
            rtbRezult.Text = "";
            // 
            // btnPaste
            // 
            btnPaste.Dock = DockStyle.Fill;
            btnPaste.Location = new Point(415, 294);
            btnPaste.Name = "btnPaste";
            btnPaste.Size = new Size(144, 24);
            btnPaste.TabIndex = 1;
            btnPaste.Text = "Paste / Convert";
            btnPaste.UseVisualStyleBackColor = true;
            btnPaste.Click += btnPaste_Click;
            // 
            // btnCopy
            // 
            btnCopy.Dock = DockStyle.Fill;
            btnCopy.Location = new Point(315, 294);
            btnCopy.Name = "btnCopy";
            btnCopy.Size = new Size(94, 24);
            btnCopy.TabIndex = 2;
            btnCopy.Text = "Copy";
            btnCopy.UseVisualStyleBackColor = true;
            btnCopy.Click += btnCopy_Click;
            // 
            // cbLang
            // 
            cbLang.Dock = DockStyle.Fill;
            cbLang.DropDownStyle = ComboBoxStyle.DropDownList;
            cbLang.FormattingEnabled = true;
            cbLang.Items.AddRange(new object[] { "Python", "C#" });
            cbLang.Location = new Point(3, 294);
            cbLang.Name = "cbLang";
            cbLang.Size = new Size(306, 23);
            cbLang.TabIndex = 3;
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ControlLightLight;
            ClientSize = new Size(562, 321);
            Controls.Add(tableLayoutPanel1);
            Name = "MainWindow";
            Text = "Code Converter";
            tableLayoutPanel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private RichTextBox rtbRezult;
        private Button btnPaste;
        private Button btnCopy;
        private ComboBox cbLang;
    }
}
