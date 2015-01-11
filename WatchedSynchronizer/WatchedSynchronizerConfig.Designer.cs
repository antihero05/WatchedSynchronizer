namespace WatchedSynchronizer
{
  partial class WatchedSynchronizerConfig
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
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WatchedSynchronizerConfig));
      this.lblDescription = new System.Windows.Forms.Label();
      this.dgvDatabases = new System.Windows.Forms.DataGridView();
      this.btnCancel = new System.Windows.Forms.Button();
      this.btnOk = new System.Windows.Forms.Button();
      ((System.ComponentModel.ISupportInitialize)(this.dgvDatabases)).BeginInit();
      this.SuspendLayout();
      // 
      // lblDescription
      // 
      this.lblDescription.AutoSize = true;
      this.lblDescription.Location = new System.Drawing.Point(9, 9);
      this.lblDescription.Name = "lblDescription";
      this.lblDescription.Size = new System.Drawing.Size(198, 13);
      this.lblDescription.TabIndex = 0;
      this.lblDescription.Text = "Add databases you want to synchronize:";
      // 
      // dgvDatabases
      // 
      this.dgvDatabases.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.dgvDatabases.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
      this.dgvDatabases.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
      this.dgvDatabases.Location = new System.Drawing.Point(12, 25);
      this.dgvDatabases.Name = "dgvDatabases";
      this.dgvDatabases.Size = new System.Drawing.Size(518, 200);
      this.dgvDatabases.TabIndex = 1;
      this.dgvDatabases.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvDatabases_CellClick);
      this.dgvDatabases.CurrentCellDirtyStateChanged += new System.EventHandler(this.dgvDatabases_CurrentCellDirtyStateChanged);
      this.dgvDatabases.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(this.dgvDatabases_RowsRemoved);
      // 
      // btnCancel
      // 
      this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnCancel.Location = new System.Drawing.Point(430, 231);
      this.btnCancel.Name = "btnCancel";
      this.btnCancel.Size = new System.Drawing.Size(100, 25);
      this.btnCancel.TabIndex = 2;
      this.btnCancel.Text = "Cancel";
      this.btnCancel.UseVisualStyleBackColor = true;
      this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
      // 
      // btnOk
      // 
      this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnOk.Location = new System.Drawing.Point(324, 231);
      this.btnOk.Name = "btnOk";
      this.btnOk.Size = new System.Drawing.Size(100, 25);
      this.btnOk.TabIndex = 3;
      this.btnOk.Text = "OK";
      this.btnOk.UseVisualStyleBackColor = true;
      this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
      // 
      // WatchedSynchronizerConfig
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(542, 266);
      this.Controls.Add(this.btnOk);
      this.Controls.Add(this.btnCancel);
      this.Controls.Add(this.dgvDatabases);
      this.Controls.Add(this.lblDescription);
      this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
      this.Name = "WatchedSynchronizerConfig";
      this.Text = "WatchedSynchronizer Configuration";
      ((System.ComponentModel.ISupportInitialize)(this.dgvDatabases)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Label lblDescription;
    private System.Windows.Forms.DataGridView dgvDatabases;
    private System.Windows.Forms.Button btnCancel;
    private System.Windows.Forms.Button btnOk;
  }
}