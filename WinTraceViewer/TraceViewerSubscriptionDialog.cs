using IdeaBlade.Core;
using System;

namespace IdeaBlade.DevTools.TraceViewer {

  /// <summary>
  /// Dialog used by the <see cref="TraceViewerForm"/> to select a TracePublisher.
  /// </summary>
  internal class TraceViewerSubscriptionDialog : System.Windows.Forms.Form {

    #region Form controls
    private System.Windows.Forms.TextBox mURLTextBox;
    private System.Windows.Forms.Button mOkButton;
    private System.Windows.Forms.Button mCancelButton;
    private System.Windows.Forms.TextBox mPortTextBox;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.TextBox mServiceNameTextBox;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.TextBox mBaseURLTextBox;
    private System.Windows.Forms.Label label1;
    private System.ComponentModel.Container components = null;
    #endregion

    public TraceViewerSubscriptionDialog(string pProtocol) {
      InitializeComponent();

      mProtocol = pProtocol;
      InitializeEvents();
    }

    private void InitializeEvents() {      
      this.mServiceNameTextBox.Text = msServiceName;
      this.mBaseURLTextBox.Text = msBaseURL;
      this.mPortTextBox.Text = msPort;

      EventHandler handler = new EventHandler(TextChangedEventHandler);
      this.mBaseURLTextBox.TextChanged+= handler;
      this.mPortTextBox.TextChanged += handler;
      this.mServiceNameTextBox.TextChanged += handler;

      TextChangedEventHandler(null, null);
    }

    internal String URL {
      get { return mURLTextBox.Text.Trim(); }
    }

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    protected override void Dispose( bool disposing ) {
      if( disposing ) {
        if(components != null) {
          components.Dispose();
        }
      }
      base.Dispose( disposing );
    }



    #region Windows Form Designer generated code
    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TraceViewerSubscriptionDialog));
      this.mURLTextBox = new System.Windows.Forms.TextBox();
      this.mOkButton = new System.Windows.Forms.Button();
      this.mCancelButton = new System.Windows.Forms.Button();
      this.mPortTextBox = new System.Windows.Forms.TextBox();
      this.label2 = new System.Windows.Forms.Label();
      this.label3 = new System.Windows.Forms.Label();
      this.mServiceNameTextBox = new System.Windows.Forms.TextBox();
      this.label4 = new System.Windows.Forms.Label();
      this.mBaseURLTextBox = new System.Windows.Forms.TextBox();
      this.label1 = new System.Windows.Forms.Label();
      this.SuspendLayout();
      // 
      // mURLTextBox
      // 
      this.mURLTextBox.Enabled = false;
      this.mURLTextBox.Location = new System.Drawing.Point(120, 96);
      this.mURLTextBox.Name = "mURLTextBox";
      this.mURLTextBox.ReadOnly = true;
      this.mURLTextBox.Size = new System.Drawing.Size(360, 20);
      this.mURLTextBox.TabIndex = 1;
      // 
      // mOkButton
      // 
      this.mOkButton.DialogResult = System.Windows.Forms.DialogResult.OK;
      this.mOkButton.Location = new System.Drawing.Point(160, 144);
      this.mOkButton.Name = "mOkButton";
      this.mOkButton.Size = new System.Drawing.Size(75, 23);
      this.mOkButton.TabIndex = 3;
      this.mOkButton.Text = "&Ok";
      // 
      // mCancelButton
      // 
      this.mCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      this.mCancelButton.Location = new System.Drawing.Point(272, 144);
      this.mCancelButton.Name = "mCancelButton";
      this.mCancelButton.Size = new System.Drawing.Size(75, 23);
      this.mCancelButton.TabIndex = 4;
      this.mCancelButton.Text = "Cancel";
      // 
      // mPortTextBox
      // 
      this.mPortTextBox.Location = new System.Drawing.Point(120, 40);
      this.mPortTextBox.Name = "mPortTextBox";
      this.mPortTextBox.Size = new System.Drawing.Size(56, 20);
      this.mPortTextBox.TabIndex = 5;
      // 
      // label2
      // 
      this.label2.Location = new System.Drawing.Point(24, 42);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(40, 16);
      this.label2.TabIndex = 6;
      this.label2.Text = "Port:";
      // 
      // label3
      // 
      this.label3.Location = new System.Drawing.Point(24, 66);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(80, 16);
      this.label3.TabIndex = 7;
      this.label3.Text = "Service Name";
      // 
      // mServiceNameTextBox
      // 
      this.mServiceNameTextBox.Location = new System.Drawing.Point(120, 64);
      this.mServiceNameTextBox.Name = "mServiceNameTextBox";
      this.mServiceNameTextBox.Size = new System.Drawing.Size(360, 20);
      this.mServiceNameTextBox.TabIndex = 8;
      // 
      // label4
      // 
      this.label4.Location = new System.Drawing.Point(24, 18);
      this.label4.Name = "label4";
      this.label4.Size = new System.Drawing.Size(72, 16);
      this.label4.TabIndex = 10;
      this.label4.Text = "Base URL";
      // 
      // mBaseURLTextBox
      // 
      this.mBaseURLTextBox.Location = new System.Drawing.Point(120, 16);
      this.mBaseURLTextBox.Name = "mBaseURLTextBox";
      this.mBaseURLTextBox.Size = new System.Drawing.Size(360, 20);
      this.mBaseURLTextBox.TabIndex = 9;
      // 
      // label1
      // 
      this.label1.Location = new System.Drawing.Point(24, 98);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(32, 16);
      this.label1.TabIndex = 11;
      this.label1.Text = "URL";
      // 
      // TraceViewerSubscriptionDialog
      // 
      this.ClientSize = new System.Drawing.Size(504, 200);
      this.Controls.Add(this.label1);
      this.Controls.Add(this.label4);
      this.Controls.Add(this.mBaseURLTextBox);
      this.Controls.Add(this.mServiceNameTextBox);
      this.Controls.Add(this.label3);
      this.Controls.Add(this.label2);
      this.Controls.Add(this.mPortTextBox);
      this.Controls.Add(this.mCancelButton);
      this.Controls.Add(this.mOkButton);
      this.Controls.Add(this.mURLTextBox);
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "TraceViewerSubscriptionDialog";
      this.ShowInTaskbar = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Subscribe To";
      this.ResumeLayout(false);
      this.PerformLayout();

    }
    #endregion


    private void TextChangedEventHandler(object sender, EventArgs e) {
      msBaseURL = mBaseURLTextBox.Text.Trim();
      msPort = mPortTextBox.Text.Trim();
      msServiceName = mServiceNameTextBox.Text.Trim();
      mURLTextBox.Text = String.Format( 
        @"{3}:{0}:{1}/{2}", msBaseURL, msPort, msServiceName, mProtocol);
    }

    private String mProtocol;
    private static String msBaseURL = @"//localhost";
    private static String msPort    = TracePublisher.DefaultPort.ToString();
    private static String msServiceName = TracePublisher.DefaultServiceName;



  }

}
