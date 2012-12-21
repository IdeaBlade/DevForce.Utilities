using IdeaBlade.Core;
using System.Windows.Forms;

namespace IdeaBlade.DevTools.TraceViewer {
  internal partial class TraceMessageDialog : Form {
    public TraceMessageDialog(TraceMessage pTraceMessage) {
      InitializeComponent();
      mTraceMessage = pTraceMessage;
      this.mIdTextBox.Text = pTraceMessage.Id.ToString();
      this.mMessageTextBox.Text = pTraceMessage.Message;
    }

    public static void Show(IWin32Window pWindow, TraceMessage pTraceMessage) {
      TraceMessageDialog dialog = new TraceMessageDialog(pTraceMessage);
      dialog.ShowDialog(pWindow);
    }

    private TraceMessage mTraceMessage;
  }
}