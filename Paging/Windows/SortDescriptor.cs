using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace IdeaBlade.Windows {

  /// <summary>
  /// Sorting information used by the <see cref="ObjectDataSource"/>.
  /// </summary>
  [ContentProperty("PropertyPath")]
  public class SortDescriptor : INotifyPropertyChanged {

    /// <summary>
    /// Fires when a property changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Create an instance of this class.
    /// </summary>
    public SortDescriptor()
      : this(string.Empty, ListSortDirection.Ascending) {
    }

    /// <summary>
    /// Create an instance of this class.
    /// </summary>
    /// <param name="propertyPath"></param>
    /// <param name="direction"></param>
    public SortDescriptor(string propertyPath, ListSortDirection direction) {
      PropertyPath = new Parameter("PropertyPath", propertyPath);
      Direction = direction;
    }

    /// <summary>
    /// Initialize parameters.
    /// </summary>
    /// <param name="elementContext"></param>
    /// <param name="culture"></param>
    public void Initialize(FrameworkElement elementContext, CultureInfo culture) {
      PropertyPath.Initialize(elementContext, culture);
    }

    /// <summary>
    /// Return a <see cref="SortDescription"/> from this instance.
    /// </summary>
    /// <returns></returns>
    public SortDescription ToSortDescription() {
      return new SortDescription(PropertyPath.Value.ToString(), Direction);
    }

    /// <summary>
    /// The sorting criterion for this descriptor.
    /// </summary>
    /// <remarks>
    /// A <see cref="ControlParameter"/> can be used to reference a UI control providing
    /// the value.
    /// </remarks>
    public Parameter PropertyPath {
      get {
        return _propertyPath;
      }
      set {
        if (_propertyPath != value) {
          if (_propertyPath != null) {
            _propertyPath.PropertyChanged -= new PropertyChangedEventHandler(PropertyPath_PropertyChanged);
          }
          _propertyPath = value;
          if (_propertyPath != null) {
            _propertyPath.PropertyChanged += new PropertyChangedEventHandler(PropertyPath_PropertyChanged);
          }
          OnPropertyChanged("PropertyPath");
        }
      }
    }

    /// <summary>
    /// The sort direction.
    /// </summary>
    public ListSortDirection Direction {
      get {
        return _direction;
      }
      set {
        if (_direction != value) {
          _direction = value;
          OnPropertyChanged("Direction");
        }
      }
    }

    private void PropertyPath_PropertyChanged(object sender, PropertyChangedEventArgs e) {
      if (e.PropertyName == "Value") {
        OnPropertyChanged("PropertyPath");
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="propertyName"></param>
    protected virtual void OnPropertyChanged(string propertyName) {
      PropertyChangedEventHandler propertyChanged = PropertyChanged;
      if (propertyChanged != null) {
        propertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }

    private ListSortDirection _direction;
    private Parameter _propertyPath;
  }

}