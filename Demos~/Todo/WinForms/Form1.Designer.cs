namespace TodoWinForms;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing) {
        if (disposing && (components != null)) {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
			this.listView1 = new System.Windows.Forms.ListView();
			this.openExplorer = new System.Windows.Forms.LinkLabel();
			this.info = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// listView1
			// 
			this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.listView1.Location = new System.Drawing.Point(14, 194);
			this.listView1.Margin = new System.Windows.Forms.Padding(6);
			this.listView1.Name = "listView1";
			this.listView1.Size = new System.Drawing.Size(1459, 665);
			this.listView1.TabIndex = 0;
			this.listView1.UseCompatibleStateImageBehavior = false;
			this.listView1.View = System.Windows.Forms.View.List;
			// 
			// openExplorer
			// 
			this.openExplorer.AutoSize = true;
			this.openExplorer.Location = new System.Drawing.Point(740, 49);
			this.openExplorer.Name = "openExplorer";
			this.openExplorer.Size = new System.Drawing.Size(171, 37);
			this.openExplorer.TabIndex = 1;
			this.openExplorer.TabStop = true;
			this.openExplorer.Text = "Hub Explorer";
			this.openExplorer.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.openExplorer_LinkClicked);
			// 
			// info
			// 
			this.info.AutoSize = true;
			this.info.Location = new System.Drawing.Point(12, 9);
			this.info.Name = "info";
			this.info.Size = new System.Drawing.Size(677, 111);
			this.info.TabIndex = 2;
			this.info.Text = "1. Start TodoHub server\r\n2. Open Hub Explorer and add records to jobs container\r\n" +
    "3. Changes are instantly reflected in the ListView below";
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(15F, 37F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1488, 874);
			this.Controls.Add(this.info);
			this.Controls.Add(this.openExplorer);
			this.Controls.Add(this.listView1);
			this.Margin = new System.Windows.Forms.Padding(6);
			this.Name = "Form1";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

    }

    #endregion

    private ListView listView1;
	private LinkLabel openExplorer;
	private Label info;
}