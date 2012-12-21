
using IdeaBlade.Core;
using IdeaBlade.Core.Reflection;
using IdeaBlade.EntityModel;
using IdeaBlade.EntityModel.Compat;
using IdeaBlade.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using LE = System.Linq.Expressions;

namespace IdeaBlade.Windows {

  /// <summary>
  /// A collection providing paging, sorting, grouping and filtering of query results.
  /// </summary>
  /// <remarks>
  /// The <b>EntityQueryPagedCollectionView</b> can be used from code behind, and view models in MVVM architectures,
  /// to provide paged results to bound controls.  It is also used internally by the <see cref="ObjectDataSource"/> when
  /// declarative specification in XAML is wanted.  
  /// <para>
  /// The <see cref="EntityQuery"/> provided in the constructor will be executed asynchronously
  /// as needed to fulfill each paging request.  Note that unless a <b>CacheOnly</b> query 
  /// strategy is defined, the first time a page is loaded it will use a DataSourceOnly
  /// fetch strategy.  Subsequent executions will use the <see cref="QueryStrategy"/> defined
  /// on the query.  Note that the query must be an <see cref="EntityQuery"/>, you can't
  /// use this collection with a <see cref="StoredProcQuery"/> or <see cref="PassthruEsqlQuery"/>.
  /// For those queries, use the .NET <see cref="PagedCollectionView"/> instead.
  /// </para>
  /// <para>
  /// The <b>EntityManager</b> of the query is used to perform query execution.  If none is
  /// specified an exception is thrown.  If the EntityManager  is not logged in, an asynchronous login with null credentials is performed.
  /// </para>
  /// <para>
  /// To use with a <b>DataPager</b> control, set or bind its Source property to an instance of the
  /// EntityQueryPagedCollectionView.  
  /// </para>
  /// </remarks>
  public class EntityQueryPagedCollectionView : IPagedCollectionView, ICollectionView, IEditableCollectionView, INotifyPropertyChanged {

    #region ctors

    // called by ObjectDataSource - set sort, grouping and filter info before calling load
    internal EntityQueryPagedCollectionView(EntityQuery query, int pageSize, int loadSize, 
      SortDescriptionCollection sortDescriptors, ObservableCollection<GroupDescription> groupDescriptors, IPredicateDescription filter) 
     : this(query, pageSize, loadSize, true, false) {
      sortDescriptors.ForEach(d => SortDescriptions.Add(d));
      groupDescriptors.ForEach(d=> GroupDescriptions.Add(d));
      SetQueryFilter(filter);
      Refresh();
    }

    /// <summary>
    /// Create an instance of this class.
    /// </summary>
    /// <param name="query">The query providing the results to the collection</param>
    /// <param name="pageSize">The number of items to be displayed per page</param>
    /// <remarks>
    /// Specify the <paramref name="query"/> which will be executed asynchronously to provide
    /// paged results.  The "load size", the number of items loaded as each page is request, will default to twice the <paramref name="pageSize"/> value.
    /// </remarks>
    public EntityQueryPagedCollectionView(EntityQuery query, int pageSize)
      : this(query, pageSize, pageSize * 2, false, true) {
    }

    /// <summary>
    /// Creates an instance of this class.
    /// </summary>
    /// <param name="query">The query providing the results to the collection</param>
    /// <param name="pageSize">The number of items to be displayed per page</param>
    /// <param name="loadSize">The number of items to be loaded when a page is requested</param>
    /// <remarks>
    /// Specify the <paramref name="query"/> which will be executed asynchronously to provide
    /// paged results. Set <paramref name="pageSize"/> and <paramref name="loadSize"/> to control the number of items displayed and loaded during a page request.
    /// If loadSize is a multiple of pageSize then "look ahead" queries are performed to cache page results.
    /// </remarks>
    public EntityQueryPagedCollectionView(EntityQuery query, int pageSize, int loadSize)
      : this(query, pageSize, loadSize, false, true) {
    }

    /// <summary>
    /// Create a new instance of this class.
    /// </summary>
    /// <param name="query">The query providing the results to the collection</param>
    /// <param name="pageSize">The number of items to be displayed per page</param>
    /// <param name="loadSize">The number of items to be loaded when a page is requested</param>
    /// <param name="deferLoad">Whether to defer load</param>
    /// <param name="addPrimaryKeySort">Whether the data should be sorted on primary key</param>
    /// <remarks>
    /// Specify the <paramref name="query"/> which will be executed asynchronously to provide
    /// paged results.  If the first page should not be immediately loaded set <paramref name="deferLoad"/>
    /// to true, and call <see cref="Refresh()"/> when ready to retrieve data.
    /// </remarks>
    public EntityQueryPagedCollectionView(EntityQuery query, int pageSize, int loadSize, bool deferLoad, bool addPrimaryKeySort) {
      SortDescriptions = new SortDescriptionCollection();
      GroupDescriptions = new ObservableCollection<GroupDescription>();

      if (AnonymousFns.IsAnonymousType(query.ElementType)) {
        throw new NotSupportedException("Anonymous types are not currently supported.  To work around this limitation, you can instead project into a custom type.");
      }

      _groupRoot = new CollectionViewGroupRoot(GroupDescriptions);

      _baseQuery = query;
      _entityManager = query.EntityManager;
      if (_entityManager == null) throw new InvalidOperationException("The supplied query must have an assigned EntityManager.");
      _pageSize = pageSize < 0 ? 20 : pageSize;
      _lookAhead = GetLookAheadCount(_pageSize, loadSize);
      _sortByPrimaryKey = addPrimaryKeySort;
      _culture = CultureInfo.CurrentCulture;
      _innerList = (IList)TypeFns.ConstructGenericInstance(typeof(List<>), query.ElementType);

      InitFields();

      if (deferLoad) {
        _deferredLoadPending = true;
      } else {
        DoInitialLoad();
      }
    }

    #endregion

    /// <summary>
    /// Event raised when a page load fails.
    /// </summary>
    /// <remarks>
    /// You can inspect the <see cref="AsyncEventArgs.Error">Error</see> property of the event arguments, and also call <see cref="AsyncEventArgs.MarkErrorAsHandled"/>
    /// so that the exception is not thrown.
    /// </remarks>
    public event EventHandler<PageLoadErrorEventArgs> PageLoadError;

    #region public methods not part of any interfaces

    // Called when ODS does a save or reject on the EM
    /// <summary>
    /// Reload the current page.
    /// </summary>
    public void RefreshCurrentPage() {
      ReloadCurrentPage();
    }

    /// <summary>
    /// Set a filter for this view.
    /// </summary>
    /// <param name="predicateDescription"></param>
    /// <remarks>
    /// The filter can be a simple or composite predicate.  See the
    /// <see cref="PredicateDescription"/> and <see cref="PredicateBuilder"/>
    /// for more information on building dynamic filter criteria.
    /// <para>
    /// After setting the filter, call <see cref="Refresh"/> to re-query and
    /// load the view.  
    /// </para>
    /// </remarks>
    public void SetQueryFilter(IPredicateDescription predicateDescription) {
      if (predicateDescription != null) {
        _filterExpression = predicateDescription.ToLambdaExpression();
      } else {
        _filterExpression = null;
      }
    }

    /// <summary>
    /// Set a filter for this view.
    /// </summary>
    /// <param name="expression"></param>
    /// <remarks>
    /// After setting the filter, call <see cref="Refresh"/> to re-query and
    /// load the view.
    /// </remarks>
    public void SetQueryFilter(LE.LambdaExpression expression) {
      _filterExpression = expression;
    }

    /// <summary>
    /// Can be used to clear the filter.
    /// </summary>
    /// <remarks>
    /// After clearing the filter, call <see cref="Refresh"/> to re-query and
    /// load the view.
    /// <para>
    /// Calling either of the <see cref="SetQueryFilter(IPredicateDescription)"/> overloads with a null value
    /// will also clear the filter.
    /// </para>
    /// </remarks>
    public void ClearQueryFilter() {
      _filterExpression = null;
    }

    #endregion

    #region private

    private int GetLookAheadCount(int pageSize, int loadSize) {

      // no lookahead wanted
      if (loadSize <= pageSize) { return 0; }

      int ct = Math.Max(0, (int)Math.Ceiling((double)loadSize / (double)pageSize)) - 1;
      return ct;
    }

    private void InitFields() {
      _queryPageMap.Clear();
      _requestedPageLoading.Clear();
      CurrentItem = null;
      CurrentPosition = -1;
      PageIndex = 0;
    }

    private void DoInitialLoad() {
      if (IsPageLoadOutstanding(0)) return;

      InitFields();
      _deferredLoadPending = false;
      _requestedPageIndex = 0;
      BeginTrackPageLoading(_requestedPageIndex);
      OnPageChanging(_requestedPageIndex);
      SetCurrentInfo(-1);

      var newFilteredQuery = BuildFilteredQuery();
      bool getTotal = Object.Equals(newFilteredQuery, _filteredQuery) == false;

      _filteredQuery = newFilteredQuery;
      _orderedQuery = BuildOrderedQuery();

      Coroutine.Start(() => InitialLoadCore(_requestedPageIndex, getTotal), (cop) => {
        if (cop.HasError) {
          var bop = (cop.Notifications.Count > 0 ? cop.Notifications.Last() : cop) as IBaseOperation;
          var args = OnPageLoadError(bop);
          if (args.IsErrorHandled) cop.MarkErrorAsHandled();
        }
      });
    }

    private IEnumerable<INotifyCompleted> InitialLoadCore(int beginPageIndex, bool getTotal) {

      // Let EM do an implicit login if necessary.  
      if (getTotal) {
        yield return GetScalarQuery().Count( op => {
          if (op.HasError) return;
          SetTotalItemCount((int)op.Result);
        });
      }

      yield return _entityManager.ExecuteQueryAsync(MakePagedQuery(beginPageIndex), op=> LoadPageComplete(op), IndexToUserState(beginPageIndex));

      foreach (var op in AddCachingActions(beginPageIndex, false)) { yield return op; }
    }


    private IEnumerable<INotifyCompleted> AddCachingActions(int requestedPageIndex, bool includeRequestedPage) {

      // Run query for the requested page, and then for any pages to be cached.  Note that a query for 
      // the requested page might be satisfied synchronously since it was already cached.

      int startIx = includeRequestedPage ? 0 : 1;

      for (int i = startIx; i <= _lookAhead; i++) {
        int curIndex = requestedPageIndex + i;
        if (IsPageLoadOutstanding(curIndex)) {
          continue;
        }
        if (i > 0 && !ShouldLoadLookAheadPage(curIndex)) {
          continue;
        }

        BeginTrackPageLoading(curIndex);
        var q = MakePagedQuery(curIndex);

        // Adjust QueryStrategy 
        var strategy = q.QueryStrategy ?? _entityManager.DefaultQueryStrategy;
        if (!(strategy.FetchStrategy == FetchStrategy.CacheOnly || strategy.FetchStrategy == FetchStrategy.DataSourceOnly)) {
          // If we don't have enough in cache for a skip, then force to ds.  (This is a guess as to what's actually in cache.)
          var orderedKeys = _queryPageMap.Keys.OrderBy(k => k);
          if (!(orderedKeys.Count() > curIndex && orderedKeys.ElementAt(curIndex) == curIndex)) {
            q = q.With(new QueryStrategy(FetchStrategy.DataSourceOnly, MergeStrategy.PreserveChanges));
          }
        }
        
        yield return _entityManager.ExecuteQueryAsync(q, op => LoadPageComplete(op), IndexToUserState(curIndex));
      }
    }

    private void LoadPage(int pageIndex) {
      Coroutine.Start(() => AddCachingActions(pageIndex, true), cop => {
        if (cop.HasError) {
          var bop = (cop.Notifications.Count > 0 ? cop.Notifications.Last() : cop) as IBaseOperation;
          var args = OnPageLoadError(bop);
          if (args.IsErrorHandled) cop.MarkErrorAsHandled();
        }
      });
    }

    private PageLoadErrorEventArgs OnPageLoadError(IBaseOperation op) {
      var handler = PageLoadError;
      var args = new PageLoadErrorEventArgs(op.Error);
      if (handler != null) {
        handler(this, args);
      }
      return args;
    }

    private bool ShouldLoadLookAheadPage(int pageIndex) {
      if (pageIndex > LastPageIndex && ItemCount > 0) { return false; }
      if (_queryPageMap.ContainsKey(pageIndex)) { return false; }
      return true;
    }

    private void ReloadCurrentPage() {
      // called after an edit/add to re-apply sorting, grouping, filtering to the current page
      IsPageChanging = true;
      _requestedPageIndex = PageIndex;
      _tryRefreshItem = CurrentItem;
      LoadPage(PageIndex);
    }

    private int UserStateToIndex(object userState) {
      return Convert.ToInt32(userState.ToString().Split('-').Last());
    }
    private string IndexToUserState(int index) {
      return string.Format("{0}-{1}-{2}", this.GetHashCode(), DateTime.Now.Ticks, index);
    }

    private void LoadPageComplete(EntityQueryOperation args) {
      int pageIndex = UserStateToIndex(args.UserState);
      EndTrackPageLoading(pageIndex);

      if (args.HasError && !args.IsErrorHandled) {
        IsPageChanging = false;
        _tryRefreshItem = null;
        return;
      }

      // Only continue if this is the query for the page wanted right now.
      if (pageIndex != _requestedPageIndex) {
        return;
      }

      _requestedPageIndex = -1;
      IsPageChanging = true;
      PageIndex = pageIndex;

      // Innerlist is reloaded with each fetch - this is what will be displayed in bound control
      this._innerList.Clear();
      foreach (var item in args.Results) {
        this._innerList.Add(item);
      }

      DoGrouping();

      IsPageChanging = false;
      OnPageChanged();
      OnPropertyChanged("PageIndex");
      OnCollectionChanged(NotifyCollectionChangedAction.Reset, null, -1);

      // Reset position.  For a refresh of a page will try to reset to last selected item.
      SetCurrentInfo(GetTryRefreshPosition());
    }

    private int GetTryRefreshPosition() {
      if (_tryRefreshItem == null) return 0;
      int pos = Math.Max(_innerList.IndexOf(_tryRefreshItem), 0);
      _tryRefreshItem = null;
      return pos;
    }

    private void DoGrouping() {
      _isGrouping = false;
      if (GroupDescriptions.Count == 0) return;

      _groupRoot.BuildGroupsFromList(this._innerList);
      _isGrouping = true;
      OnCollectionChanged(NotifyCollectionChangedAction.Reset, null, -1);
    }

    private void BeginTrackPageLoading(int pageIndex) {
      _requestedPageLoading.Add(pageIndex);
    }
    private void EndTrackPageLoading(int pageIndex) {
      _requestedPageLoading.Remove(pageIndex);
    }

    private bool IsPageLoadOutstanding(int pageIndex) {
      return _requestedPageLoading.Contains(pageIndex);
    }
 
    private void SetTotalItemCount(int count) {
      _totalItemCount = count;
      OnPropertyChanged("ItemCount");
      OnPropertyChanged("TotalItemCount");
      OnPropertyChanged("IsEmpty");
    }

    private bool SetCurrentInfo(int newPosition) {

      if (!OnCurrentChanging()) { return false; }

      if (_innerList.Count > 0 && newPosition >=0 ) {
        CurrentItem = _innerList[newPosition];
        CurrentPosition = newPosition;
      } else {
        CurrentItem = null;
        CurrentPosition = -1;
      }

      OnCurrentChanged();
      OnPropertyChanged("CurrentItem");
      OnPropertyChanged("CurrentPosition");
      OnPropertyChanged("IsCurrentAfterLast");
      OnPropertyChanged("IsCurrentBeforeFirst");

      return true;
    }

#endregion

    #region Query expression handling 

    /// <summary>
    /// Build a scalar query from the original query.
    /// </summary>
    private IEntityScalarQuery GetScalarQuery() {

      EntityQuery query = _filteredQuery;
      var newQuery = query.AsScalarAsync();

      bool isCacheOnly = query.QueryStrategy != null &&
            query.QueryStrategy.FetchStrategy == FetchStrategy.CacheOnly;

      if (!isCacheOnly) {
        newQuery.QueryStrategy = QueryStrategy.DataSourceOnly;
      }

      return newQuery;
    }

    /// <summary>
    /// Build a paged query based on the current ordered query.
    /// </summary>
    private IEntityQuery MakePagedQuery(int requestedPageIndex) {
      if (_queryPageMap.ContainsKey(requestedPageIndex)) {
        return _queryPageMap[requestedPageIndex];
      }

      EntityQuery query = _orderedQuery;
      var newExpr = query.Expression;
      Type elementType = query.ElementType;
      int skip = _pageSize * requestedPageIndex;
      int take = _pageSize;
     
      if (skip > 0) {
        var constantExpr = LE.Expression.Constant(skip, typeof(int));
        newExpr = LE.Expression.Call(typeof(Queryable), "Skip",
          new Type[] { elementType }, query.Expression, constantExpr);
      }

      if (take > 0) {
        var constantExpr2 = LE.Expression.Constant(take, typeof(int));
        newExpr = LE.Expression.Call(typeof(Queryable), "Take",
          new Type[] { elementType }, newExpr, constantExpr2);
      }

      var newQuery = (EntityQuery)TypeFns.ConstructGenericInstance(typeof(EntityQuery<>),
          new Type[] { elementType }, newExpr, query);

      _queryPageMap.Add(requestedPageIndex, newQuery);

      bool isCacheOnly = _baseQuery.QueryStrategy != null &&
          _baseQuery.QueryStrategy.FetchStrategy == FetchStrategy.CacheOnly;
      // Force to DS when new, but do not store in map with this QS.
      if (!isCacheOnly) {
        newQuery = newQuery.With(new QueryStrategy(FetchStrategy.DataSourceOnly, MergeStrategy.PreserveChanges));
      }
      return newQuery;
    }

    private EntityQuery BuildFilteredQuery() {

      if (_filterExpression != null) {
        var expr = LE.Expression.Call(typeof(Queryable), "Where",
                      new Type[] { _baseQuery.ElementType },
                      _baseQuery.Expression, LE.Expression.Quote(_filterExpression));
        var query = (EntityQuery)TypeFns.ConstructGenericInstance(typeof(EntityQuery<>),
          new Type[] { _baseQuery.ElementType },
          expr, _baseQuery);
        return query;
      } else {
        return _baseQuery;
      }
    }

    private bool IsEntityQuery {
      get {
        return EntityMetadataStore.Instance.IsEntityType(this._baseQuery.ElementType);
      }
    }

    /// <summary>
    /// Build a query with OrderBy, ThenBy operators.
    /// </summary>
    private EntityQuery BuildOrderedQuery() {

      EntityQuery query = _filteredQuery;

      if (_sortByPrimaryKey && IsEntityQuery) {
        // Note that adding to SortDescriptions marks the sort column in the DataGrid too - 
        //  not sure if this behavior is wanted ...
        var keys = EntityMetadataStore.Instance.GetEntityMetadata(query.ElementType).KeyProperties;
        keys.ForEach(k => SortDescriptions.Add(new SortDescription(k.Name, ListSortDirection.Ascending)));
      }

      if (SortDescriptions.Count == 0 && GroupDescriptions.Count == 0) {
        return query;
      }

      Type elementType = query.ElementType;  // or queryabletype???
      var newExpr = query.Expression;

      var mce = newExpr as LE.MethodCallExpression;
      bool firstExpr =  ! (mce != null && mce.Method.Name == "OrderBy") ;

      foreach (SortDescription sd in GetSortDescriptionList()) {

        LE.ParameterExpression parm = LE.Expression.Parameter(elementType, string.Empty);
        LE.Expression prop = GetMemberExpression(parm, sd.PropertyName);
        LE.Expression lambda = LE.Expression.Lambda(prop, parm);

        string methodName = firstExpr ? "OrderBy" : "ThenBy";
        if (sd.Direction == ListSortDirection.Descending) {
          methodName += "Descending";
        }

        Type guess = prop.Type;
        newExpr = LE.Expression.Call(typeof(Queryable), methodName,
               new Type[] { elementType, guess }, newExpr, lambda);

        firstExpr = false;
      }

      var newQuery = (EntityQuery)TypeFns.ConstructGenericInstance(typeof(EntityQuery<>),
          new Type[] { query.ElementType }, newExpr, query);

      return newQuery;
    }

    private IList<SortDescription> GetSortDescriptionList() {

      // Build combined list of group and sort info 

      List<SortDescription> sortList = new List<SortDescription>();

      foreach (GroupDescription gd in GroupDescriptions) {
        PropertyGroupDescription pgd = gd as PropertyGroupDescription;
        Nullable<SortDescription> sd = null;
        string groupName = null;
        if (pgd != null) {
          sd = SortDescriptions.FirstOrDefault(s => s.PropertyName == pgd.PropertyName);
          groupName = pgd.PropertyName;
        } else {
          groupName = gd.GroupNames.ToAggregateString(".");
        }
        if (sd.Value.PropertyName != null) {
          sortList.Add(sd.Value);
        } else {
          sortList.Add(new SortDescription(groupName, ListSortDirection.Ascending));
        }
      }

      foreach (SortDescription sd in SortDescriptions) {
        if (!sortList.Contains(sd)) {
          sortList.Add(sd);
        }
      }

      return sortList;
    }

    private LE.Expression GetMemberExpression(LE.Expression parm, string path) {
      Type t = parm.Type;

      foreach (var propName in path.Split('.')) {
        var pi = t.GetProperty(propName);
        if (pi == null) {
          throw new InvalidOperationException(string.Format("Property {0} not found", path));
        }
        parm = LE.Expression.Property(parm, pi);
        t = pi.PropertyType;
      }
      return parm;
    }

    #endregion

    #region INotifyCollectionChanged Members

    /// <summary>
    /// Fires when items are added or removed from the collection, or the collection is reset.
    /// </summary>
    public event NotifyCollectionChangedEventHandler CollectionChanged;


    #endregion

    #region INotifyPropertyChanged Members

    /// <summary>
    /// Fires when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    #endregion

    #region IPagedCollectionView Members

    /// <summary>
    /// Fires before the <see cref="PageIndex"/> changes, allowing
    /// the page move to be cancelled.
    /// </summary>
    public event EventHandler<PageChangingEventArgs> PageChanging;

    /// <summary>
    /// Fires after the <see cref="PageIndex"/> has changed
    /// </summary>
    public event EventHandler<EventArgs> PageChanged;


    /// <summary>
    /// Gets or sets the number of items in a page.
    /// </summary>
    /// <remarks>
    /// The view will be refreshed automatically when the PageSize changes.
    /// </remarks>
    public int PageSize {
      get { return _pageSize; }
      set {
        if (_pageSize == value) return;
        _pageSize = value;
        Refresh();
        OnPropertyChanged("PageSize");
      }
    }

    /// <summary>
    /// Gets the current 0-based page index.
    /// </summary>
    public int PageIndex {
      get;
      private set;
    }

    private int LastPageIndex {
      get {
        return Math.Max(1, (int)Math.Ceiling((double)ItemCount / (double)PageSize)) - 1;
      }
    }

    /// <summary>
    /// Indicates if the page can be changed.
    /// </summary>
    public bool CanChangePage {
      get {
        if (IsEditingItem || IsAddingNew) return false;
        if (IsPageChanging) return false; // ??
        return ItemCount > _pageSize;
      }
    }

    /// <summary>
    /// Indicates if the page change request is in progress.
    /// </summary>
    public bool IsPageChanging {
      get { return _isPageChanging; }
      private set {
        _isPageChanging = value;
        OnPropertyChanged("IsPageChanging");
      }
    }

    /// <summary>
    /// Returns the total number of items available.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public int TotalItemCount {
      get { return _totalItemCount; }
    }

    /// <summary>
    /// Returns the number of items in the collection.
    /// </summary>
    /// <remarks>
    /// This is the same value as <see cref="TotalItemCount"/>, and is
    /// the total number of items which can be returned by the query.
    /// </remarks>
    public int ItemCount {
      get {
        return _totalItemCount;
      }
    }

    /// <summary>
    /// Move to the specified 0-based page index.
    /// </summary>
    /// <param name="pageIndex"></param>
    /// <returns></returns>
    public bool MoveToPage(int pageIndex) {
      // pageIndex is 0-based
      if (IsPageChanging) return false;
      if (pageIndex < 0 || pageIndex > LastPageIndex) return false;
      if (pageIndex == PageIndex) return false;
      if (!OnPageChanging(pageIndex)) return false;

      IsPageChanging = true;
      _requestedPageIndex = pageIndex;
      LoadPage(pageIndex);
      return true;
    }

    /// <summary>
    /// Move to the first page in the collection.
    /// </summary>
    /// <returns></returns>
    public bool MoveToFirstPage() {
      return MoveToPage(0);
    }

    /// <summary>
    /// Move to the last page in the collection.
    /// </summary>
    /// <returns></returns>
    public bool MoveToLastPage() {
      // do we want to load all pages from current to end, or just end??
      return MoveToPage(LastPageIndex);
    }

    /// <summary>
    /// Move to the next page in the collection.
    /// </summary>
    /// <returns></returns>
    public bool MoveToNextPage() {
      return MoveToPage(PageIndex + 1);
    }

    /// <summary>
    /// Move to the previous page in the collection.
    /// </summary>
    /// <returns></returns>
    public bool MoveToPreviousPage() {
      return MoveToPage(PageIndex - 1);
    }


    #endregion

    #region ICollectionView Members

    /// <summary>
    /// Fires when the <see cref="CurrentItem"/> is changing
    /// to allow the request to be cancelled.
    /// </summary>
    public event CurrentChangingEventHandler CurrentChanging;

    /// <summary>
    /// Fires when the <see cref="CurrentItem"/> has changed.
    /// </summary>
    public event EventHandler CurrentChanged;


    /// <summary>
    /// 
    /// </summary>
    public CultureInfo Culture {
      get { return _culture; }
      set {
        if (_culture != value) {
          _culture = value;
          OnPropertyChanged("Culture");
        }
      }
    }

    /// <summary>
    /// Returns true if the current page contains the specified item.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Contains(object item) {
      return _innerList.Contains(item);
    }

    /// <summary>
    /// Returns true if the collection is empty.
    /// </summary>
    public bool IsEmpty {
      get { return ItemCount <= 0; }
    }

#region Current item

    /// <summary>
    /// The currently selected item.
    /// </summary>
    public object CurrentItem {
      get ;
      private set;
    }

    /// <summary>
    /// The row index of the <see cref="CurrentItem"/>.
    /// </summary>
    public int CurrentPosition {
      get ;
      private set;
    }

    /// <summary>
    /// 
    /// </summary>
    public bool IsCurrentAfterLast {
      get { return CurrentPosition < 0 || CurrentPosition > ItemCount; }
    }

    /// <summary>
    /// 
    /// </summary>
    public bool IsCurrentBeforeFirst {
      get { return CurrentPosition < 0 || CurrentPosition > ItemCount; }
    }

    /// <summary>
    /// Moves the <see cref="CurrentPosition"/> to the position indicated.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public bool MoveCurrentToPosition(int position) {
      if (position < 0 || position > _innerList.Count - 1) return false;
      if (position == CurrentPosition) return false;

      return SetCurrentInfo(position);
    }

    /// <summary>
    /// Selects the indicated item as the <see cref="CurrentItem"/>.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool MoveCurrentTo(object item) {
      return MoveCurrentToPosition(_innerList.IndexOf(item));
    }

    /// <summary>
    /// Selects the item at position 0 as the <see cref="CurrentItem"/>.
    /// </summary>
    /// <returns></returns>
    public bool MoveCurrentToFirst() {
      return MoveCurrentToPosition(0);
    }

    /// <summary>
    /// Selects the last item on the page as the <see cref="CurrentItem"/>.
    /// </summary>
    /// <returns></returns>
    public bool MoveCurrentToLast() {
      return MoveCurrentToPosition(_innerList.Count - 1);
    }

    /// <summary>
    /// Selects the next item as the <see cref="CurrentItem"/>.
    /// </summary>
    /// <returns></returns>
    public bool MoveCurrentToNext() {
      return MoveCurrentToPosition(CurrentPosition + 1);
    }

    /// <summary>
    /// Selects the previous item as the <see cref="CurrentItem"/>.
    /// </summary>
    /// <returns></returns>
    public bool MoveCurrentToPrevious() {
      return MoveCurrentToPosition(CurrentPosition - 1);
    }

#endregion

    /// <summary>
    /// Called by bound controls when data should be refreshed.
    /// </summary>
    /// <returns></returns>
    // This will be called when bound grid is sorted
    public IDisposable DeferRefresh() {
      return new UsingBlock(() => {
        var x = this;
      }, Refresh);
    }

    /// <summary>
    /// Re-create the view, using any SortDescriptions, GroupDescriptions and/or filter.
    /// </summary>
    /// <remarks>
    /// Call <b>Refresh</b> if the EntityQueryPagedCollectionView was
    /// created with deferred loading.
    /// </remarks>
    public void Refresh() {
      if (_deferredLoadPending) {
        DoInitialLoad();
      } else {
        // Reset page now, in case DataPager is disabled when total count changes.
        _tryRefreshItem = CurrentItem;
        PageIndex = 0;
        OnPropertyChanged("PageIndex");
        SetCurrentInfo(-1);
        DoInitialLoad();
      }
    }
    

    /// <summary>
    /// The current page results.
    /// </summary>
    public IEnumerable SourceCollection {
      get {
        return this._innerList;
      }
    }

    /// <summary>
    /// Not supported.  See <see cref="SetQueryFilter"/> instead.
    /// </summary>
    /// <remarks>
    /// Filtering via the ICollectionView.Filter is not supported.  Instead you can
    /// call the <see cref="SetQueryFilter"/> method to set a filter.
    /// </remarks>
    public bool CanFilter {
      get { return false; }
    }

    /// <summary>
    /// Not supported.  See <see cref="SetQueryFilter"/> instead.
    /// </summary>
    /// <remarks>
    /// Filtering via the ICollectionView.Filter is not supported.  Instead you can
    /// call the <see cref="SetQueryFilter"/> method to set a filter.
    /// </remarks>
    public Predicate<object> Filter {
      get {
        throw new NotImplementedException();
      }
      set {
        throw new NotImplementedException();
      }
    }

    /// <summary>
    /// Indicates if the collection supports grouping.
    /// </summary>
    /// <remarks>
    /// Returns true.
    /// </remarks>
    public bool CanGroup {
      get { return true; }
    }

    /// <summary>
    /// Returns grouping information.
    /// </summary>
    public ObservableCollection<GroupDescription> GroupDescriptions {
      get ;
      private set;
    }

    /// <summary>
    /// Returns the current <see cref="SourceCollection"/> as a collection
    /// of <see cref="CollectionViewGroup"/> nodes.
    /// </summary>
    ///  where this is documented i have no idea, but this wants to be
    ///  a collection of CollectionViewGroup items
    ///  note that the datagrid will invoke this method a lot (at least whenever
    ///  ProtectedItems change - I can't get grid to correctly display groups unless 
    ///  letting it access the _groupRoot.Items every time an item is added
    public ReadOnlyObservableCollection<object> Groups {
      get {
        if (GroupDescriptions.Count == 0) { return null; }

        if (_isGrouping) {
          return _groupRoot.Items;
        } else {
          return null;
        }
      }
    }

    /// <summary>
    /// Indicates if the collection supports sorting.
    /// </summary>
    /// <remarks>
    /// Returns true.
    /// </remarks>
    public bool CanSort {
      get { return true; }
    }

    /// <summary>
    /// One or more <see cref="SortDescription"/> items.
    /// </summary>
    public SortDescriptionCollection SortDescriptions {
      get;
      private set;
    }

    #endregion

    #region IEnumerable Members

    /// <summary>
    /// Returns the collection enumerator.
    /// </summary>
    /// <returns></returns>
    public IEnumerator GetEnumerator() {
      if (_isGrouping) {
        return _groupRoot.GetLeafEnumerator();
      } else {
        return SourceCollection.GetEnumerator();
      }
    }

    #endregion

    #region IEditableCollectionView Members
    
    #region Edit handling

    /// <summary>
    /// Returns true if the <see cref="CurrentEditItem"/>
    /// implements <see cref="IEditableObject"/>.
    /// </summary>
    public bool CanCancelEdit {
      get {
        var eo = CurrentEditItem as IEditableObject;
        return eo != null;
      }
    }

    /// <summary>
    /// Indicates if an item edit is in progress.
    /// </summary>
    public bool IsEditingItem {
      get { return CurrentEditItem != null; }
    }

    /// <summary>
    /// The item current being edited.
    /// </summary>
    public object CurrentEditItem {
      get { return _editItem; }
      private set {
        _editItem = value;
        OnPropertyChanged("CurrentEditItem");
        OnPropertyChanged("IsEditingItem");
        OnPropertyChanged("CanChangePage");
        OnPropertyChanged("CanCancelEdit");
        OnPropertyChanged("CanRemove");
      }
    }

    /// <summary>
    /// Make an item the <see cref="CurrentEditItem"/>.
    /// </summary>
    /// <param name="item"></param>
    /// <remarks>
    /// If the item implements <see cref="IEditableObject"/>
    /// BeginEdit is called.
    /// </remarks>
    public void EditItem(object item) {
      if (IsAddingNew) {
        if (object.Equals(item, CurrentAddItem)) {
          return;
        }
        CommitNew();
      }
      CommitEdit();

      CurrentEditItem = item;
      IEditableObject eo = item as IEditableObject;
      if (eo != null) {
        eo.BeginEdit();
      }
    }

    /// <summary>
    /// Completes editing of the <see cref="CurrentEditItem"/>.
    /// </summary>
    /// <remarks>
    /// Calls EndEdit if the item implements <see cref="IEditableObject"/>.
    /// </remarks>
    public void CommitEdit() {
      if (CurrentEditItem == null) { return; }

      IEditableObject eo = CurrentEditItem as IEditableObject;
      if (eo != null) {
        eo.EndEdit();
      }

      CurrentEditItem = null;

      SetCurrentInfo(CurrentPosition);
      ReloadCurrentPage();
    }

    /// <summary>
    /// Cancels editing of the <see cref="CurrentEditItem"/>.
    /// </summary>
    /// <remarks>
    /// Calls CancelEdit if the item implements <see cref="IEditableObject"/>.
    /// </remarks>
    public void CancelEdit() {
      if (CurrentEditItem == null) { return; }

      IEditableObject eo = CurrentEditItem as IEditableObject;
      if (eo != null) {
        eo.CancelEdit();
      }

      CurrentEditItem = null;
    }

    #endregion

    #region New item handling

    /// <summary>
    /// Not implemented.
    /// </summary>
    public NewItemPlaceholderPosition NewItemPlaceholderPosition {
      get {
        return NewItemPlaceholderPosition.None;
      }
      set {
        throw new NotImplementedException();
      }
    }

    /// <summary>
    /// Whether new items can be added to the collection.
    /// </summary>
    public bool CanAddNew {
      get {
        if (IsEditingItem) { return false; }
        return AnonymousFns.IsAnonymousType(_baseQuery.ElementType) == false;
      }
    }

    /// <summary>
    /// Indicates if a new item is currently being created.
    /// </summary>
    public bool IsAddingNew {
      get { return CurrentAddItem != null; }
    }

    /// <summary>
    /// The item currently being added.
    /// </summary>
    public object CurrentAddItem {
      get { return _newItem; }
      private set {
        _newItem = value;
        OnPropertyChanged("CurrentAddItem");
        OnPropertyChanged("IsAddingNew");
        OnPropertyChanged("CanChangePage");
        OnPropertyChanged("CanCancelEdit");
        OnPropertyChanged("CanRemove");
      }
    }

    /// <summary>
    /// Creates an item and adds it to the collection for editing.
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// Calls BeginEdit if the item implements <see cref="IEditableObject"/>.
    /// <see cref="EntityManager.CreateEntity{T}()">EntityManager.CreateEntity</see>
    /// is used to create the item.
    /// </remarks>
    public object AddNew() {

      CommitNew();

      object item = _entityManager.CreateEntity(_baseQuery.ElementType);
      // In order to use BeginEdit the item must be attached.
      _entityManager.AddEntity(item);

      int ix = _innerList.Add(item);

      CurrentAddItem = item;

      SetTotalItemCount(_totalItemCount + 1);

      if (_isGrouping) {
        _groupRoot.AddSpecialItem(item);
      }

      SetCurrentInfo(ix);
      OnCollectionChanged(NotifyCollectionChangedAction.Add, CurrentAddItem, CurrentPosition);

      IEditableObject eo = item as IEditableObject;
      if (eo != null) {
        eo.BeginEdit();
      }
      return item;
    }

    /// <summary>
    /// Completes editing of the <see cref="CurrentAddItem"/>.
    /// </summary>
    /// <remarks>
    /// Calls EndEdit if the item implements <see cref="IEditableObject"/>.
    /// <see cref="T:IdeaBlade.EntityModel.EntityManager.AddEntity{object}">EntityManager.AddEntity</see>
    /// is called to add the item to the EntityManager cache.
    /// </remarks>
    public void CommitNew() {
      if (CurrentAddItem == null) { return; }

      IEditableObject eo = CurrentAddItem as IEditableObject;
      if (eo != null) {
        eo.EndEdit();
      }

      _entityManager.AddEntity(CurrentAddItem);

      if (_isGrouping) {
        _groupRoot.RemoveSpecialItem(CurrentAddItem);
      }

      CurrentAddItem = null;
      SetCurrentInfo(CurrentPosition);
      ReloadCurrentPage();
    }

    /// <summary>
    /// Cancels editing of the <see cref="CurrentAddItem"/>.
    /// </summary>
    /// <remarks>
    /// Calls CancelEdit if the item implements <see cref="IEditableObject"/>.
    /// </remarks>
    public void CancelNew() {
      if (CurrentAddItem == null) { return; }

      var item = CurrentAddItem;

      IEditableObject eo = CurrentAddItem as IEditableObject;
      if (eo != null) {
        eo.CancelEdit();
      }

      CurrentAddItem = null;
      RemoveItemFromList(item, true);
      _entityManager.RemoveEntity(item);
    }

    #endregion

    #region Remove handling

    /// <summary>
    /// Indicates if items can be removed from the collection.
    /// </summary>
    public bool CanRemove {
      get {
        if (IsAddingNew || IsEditingItem || _innerList.Count == 0) { return false; }
        return true; 
      }
    }

    /// <summary>
    /// Removes an item from the page and calls Delete on the item.
    /// </summary>
    /// <param name="item"></param>
    public void Remove(object item) {

      EntityAspect.Wrap(item).Delete();
      RemoveItemFromList(item, false);
    }

    /// <summary>
    /// Removes the item at the specified row index and calls Delete on the item.
    /// </summary>
    /// <param name="index"></param>
    public void RemoveAt(int index) {
      object item = _innerList[index];
      EntityAspect.Wrap(item).Delete();
      RemoveItemFromList(item, false);
    }

    private void RemoveItemFromList(object item, bool isNew) {
      var ix = _innerList.IndexOf(item);
      _innerList.Remove(item);
      SetTotalItemCount(_totalItemCount - 1);

      if (_isGrouping) {
        if (isNew) {
          _groupRoot.RemoveSpecialItem(item);
        } else {
          _groupRoot.RemoveItemFromSubgroup(item);
        }
      }

      SetCurrentInfo(ix > 0 ? ix - 1 : 0);
      OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, ix);
    }

    #endregion

    #endregion

    #region Events

    /// <summary>
    /// Fires PageChanging event.
    /// </summary>
    /// <param name="newPageIndex"></param>
    /// <returns>true if ok to change page, otherwise cancel</returns>
    protected bool OnPageChanging(int newPageIndex) {
      if (PageChanging != null) {
        PageChangingEventArgs args = new PageChangingEventArgs(newPageIndex);
        PageChanging(this, args);
        if (args.Cancel) {
          return false;
        }
      }
      return true;
    }

    /// <summary>
    /// Fires PageChanged.
    /// </summary>
    protected void OnPageChanged() {
      if (PageChanged != null) {
        PageChanged(this, EventArgs.Empty);
      }
    }

    /// <summary>
    /// Fires PropertyChanged.
    /// </summary>
    /// <param name="propertyName"></param>
    protected void OnPropertyChanged(string propertyName) {
      if (PropertyChanged != null) {
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }

    /// <summary>
    /// Fires CollectionChanged.
    /// </summary>
    /// <param name="action"></param>
    /// <param name="item"></param>
    /// <param name="index"></param>
    protected void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index) {
      if (CollectionChanged != null) {
        NotifyCollectionChangedEventArgs args;
        if (action == NotifyCollectionChangedAction.Reset) {
          args = new NotifyCollectionChangedEventArgs(action);
        } else {
          args = new NotifyCollectionChangedEventArgs(action, item, index);
        }
        CollectionChanged(this, args);
      }
    }

    /// <summary>
    /// Fires CurrentChanging.
    /// </summary>
    /// <returns></returns>
    protected bool OnCurrentChanging() {
      if (CurrentChanging != null) {
        CurrentChangingEventArgs args = new CurrentChangingEventArgs();
        CurrentChanging(this, args);
        if (args.Cancel) {
          return false;
        }
      }
      return true;
    }

    /// <summary>
    /// Fires CurrentChanged.
    /// </summary>
    protected void OnCurrentChanged() {
      if (CurrentChanged != null) {
        CurrentChanged(this, EventArgs.Empty);
      }
    }

    #endregion

    #region Locals
    private EntityQuery _baseQuery;
    private EntityQuery _filteredQuery;
    private EntityQuery _orderedQuery;
    private LE.LambdaExpression _filterExpression;
    private EntityManager _entityManager;
    private bool _sortByPrimaryKey;
    private IList _innerList;
    private int _pageSize;
    private int _totalItemCount = -1;
    private bool _isPageChanging;
    private int _requestedPageIndex;  // note this is 0-based
    private object _editItem;
    private object _newItem;
    private bool _deferredLoadPending = false;
    private int _lookAhead;
    private bool _isGrouping = false;
    private CollectionViewGroupRoot _groupRoot;
    private object _tryRefreshItem;
    private CultureInfo _culture;

    // Tracks queries in progress.
    private List<int> _requestedPageLoading = new List<int>();

    // Map of all queries built by page index.
    private Dictionary<int, IEntityQuery> _queryPageMap = new Dictionary<int, IEntityQuery>();

    #endregion
  }


}

