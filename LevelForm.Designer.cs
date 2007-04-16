namespace Flatarity
{
  partial class LevelForm
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
      if(disposing && (components != null))
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
      this.lblWhich = new System.Windows.Forms.Label();
      this.txtLevel = new System.Windows.Forms.TextBox();
      this.btnOk = new System.Windows.Forms.Button();
      this.btnCancel = new System.Windows.Forms.Button();
      this.SuspendLayout();
      // 
      // lblWhich
      // 
      this.lblWhich.AutoSize = true;
      this.lblWhich.Font = new System.Drawing.Font("Arial", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.lblWhich.Location = new System.Drawing.Point(11, 14);
      this.lblWhich.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
      this.lblWhich.Name = "lblWhich";
      this.lblWhich.Size = new System.Drawing.Size(149, 27);
      this.lblWhich.TabIndex = 0;
      this.lblWhich.Text = "Which level?";
      // 
      // txtLevel
      // 
      this.txtLevel.Location = new System.Drawing.Point(172, 14);
      this.txtLevel.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
      this.txtLevel.Name = "txtLevel";
      this.txtLevel.Size = new System.Drawing.Size(69, 29);
      this.txtLevel.TabIndex = 1;
      this.txtLevel.TextChanged += new System.EventHandler(this.txtLevel_TextChanged);
      // 
      // btnOk
      // 
      this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
      this.btnOk.Location = new System.Drawing.Point(15, 63);
      this.btnOk.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
      this.btnOk.Name = "btnOk";
      this.btnOk.Size = new System.Drawing.Size(71, 39);
      this.btnOk.TabIndex = 2;
      this.btnOk.Text = "&Go!";
      this.btnOk.UseVisualStyleBackColor = true;
      // 
      // btnCancel
      // 
      this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      this.btnCancel.Location = new System.Drawing.Point(98, 63);
      this.btnCancel.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
      this.btnCancel.Name = "btnCancel";
      this.btnCancel.Size = new System.Drawing.Size(143, 39);
      this.btnCancel.TabIndex = 3;
      this.btnCancel.Text = "Cancel";
      this.btnCancel.UseVisualStyleBackColor = true;
      // 
      // LevelForm
      // 
      this.AcceptButton = this.btnOk;
      this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 22F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = this.btnCancel;
      this.ClientSize = new System.Drawing.Size(261, 116);
      this.Controls.Add(this.btnCancel);
      this.Controls.Add(this.btnOk);
      this.Controls.Add(this.txtLevel);
      this.Controls.Add(this.lblWhich);
      this.Font = new System.Drawing.Font("Arial", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "LevelForm";
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Select Level";
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Label lblWhich;
    private System.Windows.Forms.TextBox txtLevel;
    private System.Windows.Forms.Button btnOk;
    private System.Windows.Forms.Button btnCancel;
  }
}