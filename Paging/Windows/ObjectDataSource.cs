
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
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LE = System.Linq.Expressions;


namespace IdeaBlade.Windows {

  /// <summary>
  /// A control providing paged loading, sorting, grouping and filtering of query results.
  /// <remarks>
  /// Use the <b>ObjectDataSource</b> to provide a declarative means of defining the source
  /// for data bound controls.  The <see cref="Data"/> property returns the query results.  
  /// If paging is wanted, set the <see cref="PageSize"/> to a non-zero value to indicate how
  /// many items will be displayed per page.  Use the <see cref="LoadSize"/> to do look-ahead
  /// caching of pages.
  /// <para>
  /// To provide a query to the <b>ObjectDataSource</b> declaratively, provide a value
  /// for the <see cref="QueryName"/> property, and provide either: 1) the <see cref="TypeName"/>
  /// of the fully-qualified name of the type on which the QueryName static member is defined,
  /// or 2) specify an <see cref="EntityManager"/> on which the QueryName instance or static member
  /// is defined.  The QueryName member will be called during load
  /// processing to retrieve the actual <see cref="IEntityQuery"/> to be used.  The query is run
  /// asynchronously for each page to be loaded.
  /// </para>
  /// <para>
  /// Any changes made to the data can be saved using <see cref="SaveChanges()"/>, or rejected using
  /// <see cref="RejectChanges"/>.  
  /// </para>
  /// <para>
  /// Use the <see cref="SortDescriptors"/> property to provide one or more <see cref="SortDescriptor"/>
  /// items.  Use the <see cref="GroupDescriptors"/> to specify grouping; and use
  /// the <see cref="FilterDescriptors"/> to specify any additional filters to be applied to the query.
  /// </para>
  /// <para>
  /// The <b>ObjectDataSource</b> supports several visual states: common states of "Enabled" or
  /// "Disabled", and activity states of "Idle", "Loading" and "Saving".
  /// </para>
  /// </remarks>
  /// </summary>
  [TemplateVisualState(Name = "Enabled", GroupName = "CommonState")]
  [TemplateVisualState(Name = "Disabled", GroupName = "CommonState")]
  [TemplateVisualState(Name = "Idle", GroupName = "ActivityState")]
  [TemplateVisualState(Name = "Loading", GroupName = "ActivityState")]
  [TemplateVisualState(Name = "Saving", GroupName = "ActivityState")]
  public partial class ObjectDataSource : Control {

    #region dependency properties and events

    /// <summary>
    /// See <see cref="Data"/>.
    /// </summary>
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(IEnumerable), typeof(ObjectDataSource), null);

    /// <summary>
    /// See <see cref="DataView"/>.
    /// </summary>
    public static readonly DependencyProperty DataViewProperty = DependencyProperty.Register("DataView", typeof(IPagedCollectionView), typeof(ObjectDataSource), new PropertyMetadata(new PropertyChangedCallback(DataViewChanged)));

    /// <summary>
    /// See <see cref="LoadSize"/>.
    /// </summary>
    public static readonly DependencyProperty LoadSizeProperty = DependencyProperty.Register("LoadSize", typeof(int), typeof(ObjectDataSource), new PropertyMetadata(new PropertyChangedCallback(LoadSizeChanged)));

    /// <summary>
    /// See <see cref="PageSize"/>.
    /// </summary>
    public static readonly DependencyProperty PageSizeProperty = DependencyProperty.Register("PageSize", typeof(int), typeof(ObjectDataSource), new PropertyMetadata(new PropertyChangedCallback(PageSizeChanged)));

    /// <summary>
    /// See <see cref="AutoLoad"/>.
    /// </summary>
    public static readonly DependencyProperty AutoLoadProperty = DependencyProperty.Register("AutoLoad", typeof(bool), typeof(ObjectDataSource), new PropertyMetadata(new PropertyChangedCallback(AutoLoadChanged)));

    /// <summary>
    /// See <see cref="EntityManager"/>.
    /// </summary>
    public static readonly DependencyProperty EntityManagerProperty = DependencyProperty.Register("EntityManager", typeof(EntityManager), typeof(ObjectDataSource), new PropertyMetadata(new PropertyChangedCallback(EntityManagerChanged)));

    /// <summary>
    /// See <see cref="IsBusy"/>.
    /// </summary>
    public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register("IsBusy", typeof(bool), typeof(ObjectDataSource), null);

    /// <summary>
    /// See <see cref="HasChanges"/>.
    /// </summary>
    public static readonly DependencyProperty HasChangesProperty = DependencyProperty.Register("HasChanges", typeof(bool), typeof(ObjectDataSource), null);

    /// <summary>
    /// See <see cref="IsLoadingData"/>.
    /// </summary>
    public static readonly DependencyProperty IsLoadingDataProperty = DependencyProperty.Register("IsLoadingData", typeof(bool), typeof(ObjectDataSource), null);

    /// <summary>
    /// See <see cref="IsSavingChanges"/>.
    /// </summary>
    public static readonly DependencyProperty IsSavingChangesProperty = DependencyProperty.Register("IsSavingChanges", typeof(bool), typeof(ObjectDataSource), null);

    #endregion

    #region init

    /// <summary>
    /// Creates an instance of this type.
    /// </summary>
    public ObjectDataSource() {
      this.Loaded += ObjectDataSource_Loaded;

      GroupDescriptors = new ObservableItemCollection<GroupDescriptor>();
      SortDescriptors = new ObservableItemCollection<SortDescriptor>();
      FilterDescriptors = new ObservableItemCollection<FilterDescriptor>();

      Culture = CultureInfo.CurrentCulture;

      // approach 1 - 
      //   - have an <EntityManager> element in xaml
      //   - QueryName attribute set to a method or property in the EM

      // approach 2 -
      //  - supply type and method name (static or instance?) for query and we'll call

      // code only 3 -
      //   - EntityQuery prop - 
    }

    private void ObjectDataSource_Loaded(object sender, RoutedEventArgs e) {

      if (DesignerProperties.IsInDesignTool) return;

      // Load event will fire when return to page during navigation.
      if (_controlLoaded) {
        return;
      }

      // Initialize ControlParameters in descriptors now.
      SortDescriptors.ForEach(s => s.Initialize(this, this.Culture));
      FilterDescriptors.ForEach(f => f.Initialize(this, this.Culture));
      GroupDescriptors.ForEach(g => g.Initialize(this, this.Culture));

      // We have to listen for changes.
      SortDescriptors.CollectionChanged += SortDescriptors_CollectionChanged;
      SortDescriptors.ItemChanged += SortDescriptors_ItemChanged;
      FilterDescriptors.ItemChanged += FilterDescriptors_ItemChanged;
      FilterDescriptors.CollectionChanged += FilterDescriptors_CollectionChanged;
      GroupDescriptors.ItemChanged += GroupDescriptors_ItemChanged;
      GroupDescriptors.CollectionChanged += GroupDescriptors_CollectionChanged;

      if (AutoLoad) {
        LoadCore();
      }
      _controlLoaded = true;
    }

    #endregion

    #region public methods
    /// <summary>
    /// Call to load or refresh data when <see cref="AutoLoad"/> is not on.
    /// </summary>
    public void Load() {
      LoadCore();
    }

    /// <summary>
    /// Load data, passing the source query.
    /// </summary>
    /// <param name="query"></param>
    public void Load(IEntityQuery query) {
      Query = query;
      LoadCore();
    }

    /// <summary>
    /// Rejects any changes made to data in the collection.
    /// </summary>
    /// <remarks>
    /// Note that any changes in the EntityManager will be rolled back, so use caution if a new EntityManager was not constructed for this control.
    /// </remarks>
    public void RejectChanges() {
      EntityManager.RejectChanges();
      HasChanges = false;
      RefreshViewAfterSaveOrReject();
    }

    /// <summary>
    /// Saves any data changes to the backend datasource.
    /// </summary>
    /// <remarks>
    /// An asynchronous <see cref="M:IdeaBlade.EntityModelEntityManager.SaveChanges()">SaveChanges</see>
    /// call is made on the <see cref="EntityManager"/>.  Note that any changes in the EntityManager
    /// will be saved, so use caution if a new EntityManager was not constructed for this control.
    /// You can use the <see cref="EntityManager"/> to set up event handlers.  
    /// </remarks>
    public void SaveChanges() {
      SaveChanges(args => {
        if (args.Error != null) {
          throw args.Error;
        }
      });
    }

    /// <summary>
    /// Saves any changes made to the backend datasource.
    /// </summary>
    /// <param name="callback"></param>
    /// <remarks>
    /// An asynchronous <see cref="M:IdeaBlade.EntityModelEntityManager.SaveChanges()">SaveChanges</see>
    /// call is made on the <see cref="EntityManager"/>.  Specify the <paramref name="callback"/>
    /// to be called when the save completes.  Note that any changes in the EntityManager
    /// will be saved, so use caution if a new EntityManager was not constructed for this control.
    /// You can use the <see cref="EntityManager"/> to set up event handlers.  
    /// </remarks>
    public void SaveChanges(Action<EntitySaveOperation> callback) {
      SaveChanges(null, callback);
    }

    /// <summary>
    /// Saves the specified changes to the backend datasource.
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="saveOptions"></param>
    /// <param name="callback"></param>
    /// <remarks>
    /// An asynchronous <see cref="M:IdeaBlade.EntityModelEntityManager.SaveChanges()">SaveChanges</see>
    /// call is made on the <see cref="EntityManager"/>.  Specifiy the entities to be saved, and the
    /// <see cref="SaveOptions"/> in effect.  Specify the <paramref name="callback"/>
    /// to be called when the save completes.
    /// </remarks>
    public void SaveChanges(SaveOptions saveOptions, Action<EntitySaveOperation> callback) {
      IsSavingChanges = true;
      EntityManager.SaveChangesAsync(saveOptions, (args) => {
        IsSavingChanges = false;
        HasChanges = args.Error != null || !args.SaveResult.Ok;
        if (callback != null) { callback(args); }
        if (args.Error == null && args.SaveResult.Ok) { RefreshViewAfterSaveOrReject(); }
      }, null);
    }

    /// <summary>
    /// Applies visual state changes for templating.
    /// </summary>
    public override void OnApplyTemplate() {
      base.OnApplyTemplate();
      ApplyState(false);
    }

    #endregion

    #region public properties

    /// <summary>
    /// The source query to be executed.
    /// </summary>
    /// <remarks>
    /// You can set this from code behind if wanted, otherwise provide a <see cref="QueryName"/>
    /// indicating how the query will be retrieved.
    /// </remarks>
    public IEntityQuery Query {
      get;
      set;
    }

    /// <summary>
    /// The fully-qualified type name of the type where the <see cref="QueryName"/> member can be found.  
    /// </summary>
    /// <remarks>
    /// Not required if the QueryName is a member on the <see cref="EntityManager"/>.
    /// <para>
    /// In most cases you will need to provide a fully-qualified type name, including assembly version.  For example:
    /// <code>
    /// MySilverlightApp.MainPage, MySilverlightApp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    /// </code>
    /// </para>
    /// </remarks>
    public string TypeName {
      get;
      set;
    }

    /// <summary>
    /// Name of member which when invoked will return the <see cref="IEntityQuery"/> to be used as the data source.  
    /// </summary>
    /// <remarks>
    /// The member can be an instance member of an EntityManager, or a static member of the <see cref="TypeName"/> type 
    /// (an instance of the type will not be constructed).  The member can be either a property or method.
    /// </remarks>
    public string QueryName {
      get;
      set;
    }

    /// <summary>
    /// Specify one or more <see cref="SortDescriptor"/> items to control sorting of the data.
    /// </summary>
    public ObservableItemCollection<SortDescriptor> SortDescriptors {
      get;
      private set;
    }

    /// <summary>
    /// Specify one or more <see cref="FilterDescriptor"/> items to control filtering of the data.
    /// </summary>
    /// <remarks>
    /// Filters are always ANDed together at this time.
    /// </remarks>
    public ObservableItemCollection<FilterDescriptor> FilterDescriptors {
      get;
      private set;
    }

    /// <summary>
    /// Specify one or more <see cref="GroupDescriptor"/> items to control grouping of the data.
    /// </summary>
    public ObservableItemCollection<GroupDescriptor> GroupDescriptors {
      get;
      private set;
    }

    /// <summary>
    /// Specify the number of items to be loaded at one time by a paged query.
    /// </summary>
    /// <remarks>
    /// You can set <b>LoadSize</b> to a multiple of <see cref="PageSize"/> to support
    /// lookahead caching of page results.  For example, if the PageSize is 20 and the
    /// LoadSize is 40, when a page is requested a query for the next page will also
    /// be issued, so that navigation to that next page will be from cached results.
    /// <para>
    /// Any changes made to this property after load has occurred will be ignored.
    /// </para>
    /// </remarks>
    public int LoadSize {
      get { return (int)this.GetValue(LoadSizeProperty); }
      set { this.SetValue(LoadSizeProperty, value); }
    }

    /// <summary>
    /// Specify the number of items to be displayed in a page.
    /// </summary>
    /// <remarks>
    /// Any changes made to this property after load has occurred will be ignored.
    /// </remarks>
    public int PageSize {
      get { return (int)this.GetValue(PageSizeProperty); }
      set { this.SetValue(PageSizeProperty, value); }
    }

    /// <summary>
    /// Whether to load data as soon as the control is loaded or whenever properties
    /// affecting the query change.
    /// </summary>
    /// <remarks>
    /// If false, then the <see cref="Load()"/> method should be called when ready to load or refresh
    /// the data displayed.
    /// <para>
    /// <see cref="FilterDescriptor">FilterDescriptors</see>, <see cref="SortDescriptor">SortDescriptors</see>
    /// and <see cref="GroupDescriptor">GroupDescriptors</see> affect the query and the return results.
    /// When <b>AutoLoad</b> is true, any changes to these descriptors cause an immediate refresh; when the
    /// setting is false, call <see cref="Load()"/> to refresh the data.
    /// </para>
    /// <para>
    /// Setting this property after data is loaded has no effect.
    /// </para>
    /// </remarks>
    public bool AutoLoad {
      get { return (bool)this.GetValue(AutoLoadProperty); }
      set { this.SetValue(AutoLoadProperty, value); }
    }

    /// <summary>
    /// The <see cref="T:IdeaBlade.EntityModel.EntityManager"/> against which
    /// the query is run.
    /// </summary>
    /// <remarks>
    /// If specified declaratively, a new EntityManager will be instantiated (the
    /// parameterless constructor is called).  If this is not desired, you
    /// can use the <see cref="UseDefaultManager"/> to indicate that the current
    /// <see cref="P:IdeaBlade.EntityModel.EntityManager.DefaultManager"/> should be used.
    /// If neither a new or the DefaultManager is wanted, then you will need to 
    /// set this property in code behind to the EntityManager of your choice prior
    /// to calling <see cref="Load()"/>.  
    /// <para>
    /// If the EntityManager is not logged in, an asynchronous login with null 
    /// credentials is performed prior to loading data.
    /// </para>
    /// </remarks>
    public EntityManager EntityManager {
      get { return (EntityManager)this.GetValue(EntityManagerProperty); }
      set { this.SetValue(EntityManagerProperty, value); }
    }

    /// <summary>
    /// Indicates if the control is currently busy - either loading or saving data.
    /// </summary>
    public bool IsBusy {
      get { return (bool)this.GetValue(IsBusyProperty); }
      private set {
        this.SetValue(IsBusyProperty, value);
        ApplyState(true);
      }
    }

    /// <summary>
    /// Indicates if the control is currently loading data.
    /// </summary>
    public bool IsLoadingData {
      get { return (bool)this.GetValue(IsLoadingDataProperty); }
      private set {
        if (IsLoadingData == value) return;
        this.SetValue(IsLoadingDataProperty, value);
        IsBusy = value || IsSavingChanges;
      }
    }

    /// <summary>
    /// Indicates if the control is currently saving changed data.
    /// </summary>
    public bool IsSavingChanges {
      get { return (bool)this.GetValue(IsSavingChangesProperty); }
      private set {
        if (IsSavingChanges == value) return;
        this.SetValue(IsSavingChangesProperty, value);
        IsBusy = value || IsLoadingData;
      }
    }

    /// <summary>
    /// Indicates if there are any outstanding changes to loaded data.
    /// </summary>
    public bool HasChanges {
      get { return (bool)this.GetValue(HasChangesProperty); }
      private set {
        if (HasChanges == value) return;
        this.SetValue(HasChangesProperty, value);
      }
    }

    /// <summary>
    /// The collection holding the query results.
    /// </summary>
    /// <remarks>
    /// The collection implements <see cref="IPagedCollectionView"/>.
    /// <para>
    /// Both <see cref="Data"/> and <see cref="DataView"/> return the same collection.
    /// </para>
    /// </remarks>
    public IEnumerable Data {
      get {
        return (IEnumerable)this.GetValue(DataProperty);
      }
      private set {
        this.SetValue(DataProperty, value);
      }
    }

    /// <summary>
    /// The collection holding the query results.
    /// </summary>
    /// <remarks>
    /// The collection will be either an <see cref="EntityQueryPagedCollectionView"/> or a simple
    /// <see cref="PagedCollectionView"/> depending on whether paging is wanted and the type of 
    /// query used.  Only an <see cref="EntityQuery"/> can be used with the <see cref="EntityQueryPagedCollectionView"/>.
    /// <para>
    /// Both <see cref="Data"/> and <see cref="DataView"/> return the same collection.
    /// </para>
    /// </remarks>
    public IPagedCollectionView DataView {
      get {
        return (IPagedCollectionView)this.GetValue(DataViewProperty);
      }
      private set {
        this.SetValue(DataViewProperty, value);
      }
    }

    /// <summary>
    /// Gets or sets the culture information.
    /// </summary>
    /// <remarks>
    /// BETA - not currently used.
    /// </remarks>
    public CultureInfo Culture {
      get;
      set;
    }
    #endregion

    #region private methods

    private void LoadCore() {

      GetQuery();

      // Final try for EntityManager
      if (EntityManager == null) {
        throw new InvalidOperationException("An EntityManager is required.");
      }

      LoadView();
    }

    private void GetQuery() {
      if (Query != null) return;

      Type t = null;
      object target = null;

      if (!string.IsNullOrEmpty(TypeName)) {
        t = Type.GetType(TypeName);
      } else {
        if (EntityManager != null) {
          t = EntityManager.GetType();
          target = EntityManager;
        }
      }

      if (t == null) {
        string msg = null;
        if (string.IsNullOrEmpty(TypeName)) {
          msg = "TypeName not specified and EntityManager not found.";
        } else {
          msg = string.Format("TypeName '{0}' not found", TypeName);
        }
        throw new ArgumentException(msg);
      }

      var mi = t.GetMember(QueryName).FirstOrDefault();

      if (mi is MethodInfo) {
        var method = mi as MethodInfo;
        Query = (IEntityQuery)method.Invoke(target, null);
      } else if (mi is PropertyInfo) {
        var prop = mi as PropertyInfo;
        Query = (IEntityQuery)prop.GetValue(target, null);
      } else {
        throw new ArgumentException(string.Format("QueryName '{0}' not found.", QueryName));
      }
    }

    private bool IsEntityQuery {
      get {
        return EntityMetadataStore.Instance.IsEntityType(Query.ElementType);
      }
    }

    private void LoadView() {
      // Create and start loading the PCV.

      // Neither EQPCV nor PCV will work with anonymous types.
      if (AnonymousFns.IsAnonymousType(Query.ElementType)) {
        throw new NotSupportedException("Anonymous types are not currently supported.  To work around this limitation, you can instead project into a custom type.");
      }

      if (IsEntityQuery) {
        EntityManager.GetEntityGroup(Query.ElementType).EntityChanged += ObjectDataSource_EntityChanged;
      }

      IsLoadingData = true;
      HasChanges = false;

      // Convert from our "descriptors" to the "descriptions" known by a PCV.
      var sortFields = new SortDescriptionCollection();
      SortDescriptors.ForEach(s => sortFields.Add(s.ToSortDescription()));
      var groupFields = new ObservableCollection<GroupDescription>();
      GroupDescriptors.ForEach(g => groupFields.Add(g.ToGroupDescription()));

      // We'll use an EQPCV or PCV depending on a few factors: 1) an EntityQuery (so we can issue skip/take, 2) paging
      bool useEqpcv = Query is EntityQuery && PageSize > 0;

      if (useEqpcv) {
        EntityQueryPagedCollectionView pcv = null;
        var query = Query as EntityQuery;
        var filter = GetQueryFilter();
        if (sortFields.Count > 0 || groupFields.Count > 0) {
          pcv = new EntityQueryPagedCollectionView(query, PageSize, LoadSize, sortFields, groupFields, filter);
        } else {
          pcv = new EntityQueryPagedCollectionView(query, PageSize, LoadSize, true, true);
          pcv.SetQueryFilter(filter);
          pcv.Refresh();
        }
        Data = pcv;
        DataView = pcv;

      } else {
        // Use the standard PagedCollectionView (when paging isn't wanted or not an EntityQuery)
        IEntityQuery query = Query;
        if (Query is EntityQuery) {
          query = GetFilteredQuery();
        }
        EntityManager.ExecuteQueryAsync(query, args => {
          PagedCollectionView pcv = new PagedCollectionView(args.Results);
          sortFields.ForEach(d => pcv.SortDescriptions.Add(d));
          groupFields.ForEach(d => pcv.GroupDescriptions.Add(d));
          pcv.PageSize = PageSize;
          Data = pcv;
          DataView = pcv;
        });
      }
    }

    private void RefreshViewAfterFilterChange() {
      var view = DataView as EntityQueryPagedCollectionView;
      if (view != null) {
        view.SetQueryFilter(GetQueryFilter());
        if (AutoLoad) {
          view.Refresh();
        }
      } else {
        // We use a standard PCV when the Query is not an EntityQuery (eg it's a stored proc or passthru) since
        // we can't modify the query for skip/take/etc.  The PCV does support a Predicate filter.
        var pcv = DataView as PagedCollectionView;
        pcv.Filter = GetBasicFilter();
        return;
      }
    }

    private void RefreshViewAfterSortChange() {
      var view = DataView as ICollectionView;
      view.SortDescriptions.Clear();
      SortDescriptors.ForEach(s => view.SortDescriptions.Add(s.ToSortDescription()));
      if (AutoLoad) {
        view.Refresh();
      }
    }

    private void RefreshViewAfterGroupChange() {
      var view = DataView as ICollectionView;
      view.GroupDescriptions.Clear();
      GroupDescriptors.ForEach(g => view.GroupDescriptions.Add(g.ToGroupDescription()));
      if (AutoLoad) {
        view.Refresh();
      }
    }

    private void RefreshViewAfterSaveOrReject() {
      var view = DataView as EntityQueryPagedCollectionView;
      if (view != null) {
        view.RefreshCurrentPage();
        return;
      } else {
        var icview = DataView as ICollectionView;
        icview.Refresh();
      }
    }

    private EntityQuery GetFilteredQuery() {
      var predicate = GetQueryFilter();
      return predicate == null ? (EntityQuery)Query : (EntityQuery)PredicateBuilder.FilterQuery((IQueryable)Query, predicate);
    }

    private IPredicateDescription GetQueryFilter() {
      try {
        List<PredicateDescription> predicates = new List<PredicateDescription>();

        foreach (var fd in FilterDescriptors) {
          var p = fd.ToPredicateDescription(Query.ElementType);
          if (p != null) predicates.Add(p);
        }

        if (predicates.Count > 0) {
          return PredicateBuilder.And(predicates.ToArray());
        } else {
          return null;
        }
      } catch (Exception ex) {
        TraceFns.WriteLine("Filters ignored: " + ex.Message);
        return null;
      }
    }

    private Predicate<object> GetBasicFilter() {
      var filter = GetQueryFilter();
      if (filter == null) return null;
      var converter = new PredicateDescriptionToPredicateConverter();
      return converter.Convert(filter);
    }

    #endregion

    #region visual state

    private void ApplyState(bool useTransitions) {

      // Common states = enabled, disabled
      if (IsEnabled) {
        VisualStateManager.GoToState(this, "Enabled", useTransitions);
      } else {
        VisualStateManager.GoToState(this, "Disabled", useTransitions);
      }

      // Activity states = idle, loading, saving
      bool activityOK = false;
      if (IsLoadingData) {
        activityOK = VisualStateManager.GoToState(this, "Loading", useTransitions);
      }

      if (IsSavingChanges) {
        activityOK = VisualStateManager.GoToState(this, "Saving", useTransitions);
      }

      if (!IsBusy || !activityOK) {
        VisualStateManager.GoToState(this, "Idle", useTransitions);
      }
    }

    #endregion

    #region event handlers

    #region listeners for HasChanges, IsLoadingData

    // This is a very gross way of attempting to listen for changes.  This will listen
    // for any changes in the EntityGroup holding the type of entity displayed - so any
    // changes made to entities of this type outside of the ODS's purview will still be
    // flagged here.
    private void ObjectDataSource_EntityChanged(object sender, EntityChangedEventArgs e) {
      if (IsLoadingData) return;
      if (e.Action == EntityAction.Remove || e.Action == EntityAction.Rollback) {
        HasChanges = EntityManager.HasChanges();
        return;
      }
      if (HasChanges) return;
      if ((e.Action == EntityAction.Add) ||
         (e.Action & (EntityAction.Delete | EntityAction.Change)) > 0) {
        HasChanges = true;
      }
    }

    // This is an ugly way of setting the IsLoading property - we assume, 
    // not entirely correctly, that data is "loading" as pages are changing.
    // Note this doesn't indicate that the DS is being queried, just that 
    // data is "loading".  We don't otherwise know, except maybe by listening
    // on the entity manager, when a fetch is occurring.  Right now, the 
    // IsLoading is only here to mimic the RIA DDS and to set the IsBusy flag.

    private void pcv_PageChanging(object sender, PageChangingEventArgs e) {
      IsLoadingData = true;
    }
    private void pcv_PageChanged(object sender, EventArgs e) {
      IsLoadingData = false;
    }
    #endregion

    #region Listen for Sort/group/filter descriptor changes

    private void FilterDescriptors_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      RefreshViewAfterFilterChange();
    }

    private void FilterDescriptors_ItemChanged(object sender, ItemChangedEventArgs<FilterDescriptor> e) {
      RefreshViewAfterFilterChange();
    }

    private void SortDescriptors_ItemChanged(object sender, ItemChangedEventArgs<SortDescriptor> e) {
      // todo - should relationship be 2-way?  should changes to views sortdescriptions
      // echo back to our SortDescriptors????
      // note with a grid that the view's SDs are what change automatically ...

      RefreshViewAfterSortChange();
    }

    private void SortDescriptors_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      RefreshViewAfterSortChange();
    }

    private void GroupDescriptors_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      RefreshViewAfterGroupChange();
    }

    private void GroupDescriptors_ItemChanged(object sender, ItemChangedEventArgs<GroupDescriptor> e) {
      RefreshViewAfterGroupChange();
    }

    #endregion

    #region dependency property changed handlers

    private static void DataViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
      // Fires when we set the DataView dependency property to a new pcv

      var ods = d as ObjectDataSource;
      var view = e.OldValue as IPagedCollectionView;

      if (view != null) {
        view.PageChanging -= ods.pcv_PageChanging;
        view.PageChanged -= ods.pcv_PageChanged;
      }

      view = e.NewValue as IPagedCollectionView;
      view.PageChanging += ods.pcv_PageChanging;
      view.PageChanged += ods.pcv_PageChanged;
    }


    private static void LoadSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {

    }

    private static void PageSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {

    }

    private static void AutoLoadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
    }

    private static void EntityManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
      // disallow after first load ...?

      var em = e.OldValue as EntityManager;
      if (em != null) {
        throw new InvalidOperationException("The EntityManager cannot be set after initial load.");
      }
    }


    #endregion

    #endregion

    #region locals
    private bool _controlLoaded;
    #endregion

    #region Predicate converter for standard PCV
    /// <summary>
    /// Used only when the ODS view is a standard PCV.  Converts a PredicateDescription to a predicate and also
    /// adds a coalesce operator to handle nulls.
    /// </summary>
    private class PredicateDescriptionToPredicateConverter : IdeaBlade.Linq.ExpressionVisitor {

      public Predicate<object> Convert(IPredicateDescription predicateDescription) {
        // Visit adds coalesce operator where needed
        var initExpr = predicateDescription.ToLambdaExpression();
        //var log1 = IdeaBlade.Linq.ExpressionVisitor.GetLog(initExpr);
        var expr = base.Visit(initExpr);
        //var log2 = IdeaBlade.Linq.ExpressionVisitor.GetLog(expr);

        // Now convert to Predicate<object>
        var parmExpr = LE.Expression.Parameter(typeof(Object));
        var castExpr = LE.Expression.Convert(parmExpr, predicateDescription.InstanceType);
        var invokeExpr = LE.Expression.Invoke(expr, castExpr);
        var lambdaExpr = LE.Expression.Lambda(invokeExpr, parmExpr);
        var func = (Func<Object, bool>)lambdaExpr.Compile();
        return (Object o) => func(o);
      }

      protected override LE.Expression VisitMemberAccess(LE.MemberExpression me, LE.Expression expr) {
        var pi = me.Member as PropertyInfo;
        if (pi != null && pi.PropertyType == typeof(String)) {
          var coalesceExpr = LE.Expression.Coalesce(me, LE.Expression.Constant(String.Empty));
          return coalesceExpr;
        } else {
          return base.VisitMemberAccess(me, expr);
        }
      }

      protected override LE.Expression VisitCall(LE.MethodCallExpression mce, LE.Expression objectExpr, IEnumerable<LE.Expression> argExpressions) {
        if (objectExpr != null && mce.Method.DeclaringType == typeof(String)) {
          return LE.Expression.Call(objectExpr, mce.Method, argExpressions);
        }
        return base.VisitCall(mce, objectExpr, argExpressions);
      }

      protected override LE.Expression VisitBinary(LE.BinaryExpression be, LE.Expression leftExpr, LE.Expression rightExpr) {
        if (be.NodeType == LE.ExpressionType.AndAlso || be.NodeType == LE.ExpressionType.OrElse) {
          return LE.Expression.AndAlso(leftExpr, rightExpr);
        } 
        return base.VisitBinary(be, leftExpr, rightExpr);
      }

      protected override LE.Expression VisitLambda(LE.LambdaExpression le, LE.Expression expr, IEnumerable<LE.ParameterExpression> parameterExpressions) {
        return LE.Expression.Lambda(expr, parameterExpressions);
      }
    }

    #endregion
  }
}
