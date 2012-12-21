using System;
using System.Text;
using System.Windows.Controls;
using IdeaBlade.EntityModel;
using System.ComponentModel;
using System.Windows;

namespace DevForceSilverlightApp {

  public partial class MainPage : UserControl {

    public MainPage() {
      InitializeComponent();
      this.Loaded += new System.Windows.RoutedEventHandler(MainPage_Loaded);
    }

    private void MainPage_Loaded(object sender, System.Windows.RoutedEventArgs e) {
      ((ICollectionView)_customerDataForm.ItemsSource).CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(MainPage_CollectionChanged);      
    }

    private void MainPage_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
      // HACK - 
      // The Customer type does not use an auto-incrementing PK, and nothing is raising an appropriate AddEntity event
      // or allowing us to indicate the action to perform when a new entity is created. (In a future release,
      // DevForce will add a means of specifying custom add and remove actions.)
      // For now, intercept the add to the collection and assign a good PK to the item(s).
      if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add) {
        foreach (Customer c in e.NewItems) {
          c.CustomerID = Guid.NewGuid();
        }
      }
    }

    private void EM_EntityServerError(object sender, EntityServerErrorEventArgs e) {
      WriteMessage(e.Exception.Message);
    }

    private void EM_Querying(object sender, EntityQueryingEventArgs e) {
      WriteMessage(string.Format("Executing query: {0}, strategy {1}", e.Query, e.Query.QueryStrategy.FetchStrategy));
    }

    private void SubmitButton_Click(object sender, System.Windows.RoutedEventArgs e) {
      _customersDataSource.SaveChanges(saveArgs => {
        if (saveArgs.Error != null) {
          MessageBox.Show(saveArgs.Error.Message);
          saveArgs.MarkErrorAsHandled();
        } else {
          MessageBox.Show("Changes saved!");
        }
      });
    }

    private void RejectButton_Click(object sender, System.Windows.RoutedEventArgs e) {
      _customersDataSource.RejectChanges();
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
