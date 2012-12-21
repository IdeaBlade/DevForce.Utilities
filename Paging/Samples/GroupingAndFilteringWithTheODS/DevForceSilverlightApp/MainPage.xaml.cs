using System;
using System.Text;
using System.Windows.Controls;
using IdeaBlade.EntityModel;

namespace DevForceSilverlightApp {

  public partial class MainPage : UserControl {

    public MainPage() {
      InitializeComponent();
    }

    private void EM_EntityServerError(object sender, EntityServerErrorEventArgs e) {
      WriteMessage(e.Exception.Message);
    }

    private void EM_Querying(object sender, EntityQueryingEventArgs e) {
      WriteMessage(string.Format("Executing query: {0}, strategy {1}", e.Query, e.Query.QueryStrategy.FetchStrategy));
    }

    private void WriteMessage(string msg) {
      _msgNumber += 1;
      var formatmsg = (String.Format("{0} {3} {1}{2}", _msgNumber.ToString("D4"), msg, Environment.NewLine, DateTime.Now.ToLongTimeString()));
      _statusTextBlock.Text += formatmsg;
      _statusMsg_ScrollViewer.ScrollToVerticalOffset(_statusMsg_ScrollViewer.ScrollableHeight);
    }

    int _msgNumber = 0;

  }
}
