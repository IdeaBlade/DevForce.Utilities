using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace IdeaBlade.Windows {

  /// <summary>
  /// Represents a parameter to a descriptor.
  /// </summary>
  [TypeConverter(typeof(ParameterTypeConverter))]
  public class Parameter : INotifyPropertyChanged {

    /// <summary>
    /// Fires when a property changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// 
    /// </summary>
    public Parameter() {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parameterName"></param>
    /// <param name="value"></param>
    public Parameter(string parameterName, object value) {
      ParameterName = parameterName;
      Value = value;
    }

    /// <summary>
    /// Site the parameter.
    /// </summary>
    /// <param name="elementContext"></param>
    /// <param name="culture"></param>
    public virtual void Initialize(FrameworkElement elementContext, CultureInfo culture) {
      this._culture = culture;
    }

    /// <summary>
    /// The name of the parameter.
    /// </summary>
    public string ParameterName {
      get {
        return this._name;
      }
      set {
        if (this._name != value) {
          this._name = value;
          OnPropertyChanged("ParameterName");
        }
      }
    }

    /// <summary>
    /// The value of the parameter.
    /// </summary>
    public virtual object Value {
      get {
        return this._value;
      }
      set {
        if (!object.Equals(this._value, value)) {
          this._value = value;
          OnPropertyChanged("Value");
        }
      }
    }

 
    /// <summary>
    /// 
    /// </summary>
    /// <param name="propertyName"></param>
    protected virtual void OnPropertyChanged(string propertyName) {
      PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
      if (propertyChanged != null) {
        propertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }

    private CultureInfo _culture;
    private string _name;
    private object _value;

  }

}