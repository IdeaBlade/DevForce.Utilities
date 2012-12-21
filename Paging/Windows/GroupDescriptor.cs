using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace IdeaBlade.Windows {

  /// <summary>
  /// Grouping information used by the <see cref="ObjectDataSource"/>.
  /// </summary>
  [ContentProperty("PropertyPath")]
  public class GroupDescriptor : INotifyPropertyChanged {

    /// <summary>
    /// Fires when a property changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Create an instance of this class.
    /// </summary>
    public GroupDescriptor()
      : this(string.Empty) {
    }

    /// <summary>
    /// Create an instance of this class.
    /// </summary>
    /// <param name="propertyPath"></param>
    public GroupDescriptor(string propertyPath) {
      PropertyPath = new Parameter("PropertyPath", propertyPath);
      //PropertyPath.ParameterName = "PropertyPath";
      //PropertyPath.Value = propertyPath;
    }

    /// <summary>
    /// Called to initialize any parameters.
    /// </summary>
    /// <param name="elementContext"></param>
    /// <param name="culture"></param>
    public void Initialize(FrameworkElement elementContext, CultureInfo culture) {
      PropertyPath.Initialize(elementContext, culture);
    }

    /// <summary>
    /// Return a <see cref="PropertyGroupDescription"/> from this instance.
    /// </summary>
    /// <returns></returns>
    public GroupDescription ToGroupDescription() {
      return new PropertyGroupDescription(PropertyPath.Value.ToString());
    }

    /// <summary>
    /// The grouping criterion for this descriptor.
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

    private void PropertyPath_PropertyChanged(object sender, PropertyChangedEventArgs e) {
      if (e.PropertyName == "Value") {
        OnPropertyChanged("PropertyPath");
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="propertyName"></param>
    protected void OnPropertyChanged(string propertyName) {
      PropertyChangedEventHandler propertyChanged = PropertyChanged;
      if (propertyChanged != null) {
        propertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }

    private Parameter _propertyPath;
  }

}