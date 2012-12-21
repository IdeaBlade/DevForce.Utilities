using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using IdeaBlade.EntityModel;
using IdeaBlade.Windows;

namespace DevForceSilverlightApp {

  public class MainPageViewModel : INotifyPropertyChanged {

    public MainPageViewModel() {
      Log = new ObservableCollection<string>();
      WriteToLog("Initializing EntityManager and connecting ...");
      _entityManager = new NorthwindIBEntities();

      PageSize = 20;
      InitializePagedView();
    }

    public EntityQueryPagedCollectionView View { get; private set; }

    public ObservableCollection<string> Log { get; private set; }

    public int PageSize { get; private set; }

    // Nothing currently binds to this ... but it's here if you want it. 
    public Customer CurrentCustomer {
      get { return _currentCustomer; }
      set {
        _currentCustomer = value; 
        RaisePropertyChanged("CurrentCustomer");
        if (_currentCustomer != null) WriteToLog(string.Format("Current customer is {0}", _currentCustomer.CompanyName));
      }
    }

    private void InitializePagedView() {

      // Let's look at all customers. The query can be of any complexity, but must be an EntityQuery<T>.  
      // If the query contains an OrderBy, that sort order will always be primary and any sort columns
      // selected by the user in the grid will be added as "ThenBy" clauses when sorting the results.

      // Don't execute the query (unless you do want the view to work with CacheOnly paging queries).  
      // The EntityQueryPagedCollectionView (PCV) will execute paged queries, first DataSourceOnly to load
      // the page, and then using the QueryStrategy in effect for the query or EM.

      // We haven't logged in with this EntityManager, so the PCV will do an implicit login with null
      // credentials before executing the query.  If you require real login credentials, then you should
      // be sure to login the EM first.

      // We're deferring the load here to add a SortDescription (instead of an OrderBy on the query).
      // Once the SortDescription is set we can load the view.

      var query = _entityManager.Customers;

      View = new EntityQueryPagedCollectionView(
               query,                  // Source of data
               PageSize,               // Page size
               PageSize,               // Load size - no lookahead caching here 
               true,                   // Whether to defer the load
               false);                 // Whether to add primary key to sort columns

      // Listen for errors now - we can catch connection/login errors when refresh occurs.
      View.PageLoadError += pcv_PageLoadError;

      // Set a sort column here - user choices in grid will override.
      View.SortDescriptions.Add(new SortDescription("CompanyName", ListSortDirection.Ascending));
      View.Refresh();

      // Listen for a few events.
      View.PageChanging += PageChanging;
      View.CurrentChanged += CurrentItemChanged;
    }

    private void pcv_PageLoadError(object sender, PageLoadErrorEventArgs e) {
      WriteToLog(e.Error.Message);
      e.MarkErrorAsHandled();
    }

    private void PageChanging(object sender, System.ComponentModel.PageChangingEventArgs e) {
      WriteToLog(string.Format("Moving to page {0}", e.NewPageIndex + 1));
    }

    private void CurrentItemChanged(object sender, EventArgs e) {
      CurrentCustomer = View.CurrentItem as Customer;
    }

    private void WriteToLog(string message) {
      Log.Insert(0, message); // insert each message at "top" of log
    }

    public event PropertyChangedEventHandler PropertyChanged = delegate { };

    private void RaisePropertyChanged(string property) {
      PropertyChanged(this, new PropertyChangedEventArgs(property));
    }

    private NorthwindIBEntities _entityManager;
    private Customer _currentCustomer;
  }
}
