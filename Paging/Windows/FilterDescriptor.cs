using IdeaBlade.Linq;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace IdeaBlade.Windows {

  /// <summary>
  /// Filtering information used by the <see cref="ObjectDataSource"/>.
  /// </summary>
  [ContentProperty("Value")]
  public class FilterDescriptor : INotifyPropertyChanged {

    /// <summary>
    /// Fires when a property changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Create an instance of this class.
    /// </summary>
    public FilterDescriptor()
      : this(string.Empty, FilterOperator.IsEqualTo, null) {
    }

    /// <summary>
    /// Create an instance of this class.
    /// </summary>
    /// <param name="propertyPath"></param>
    /// <param name="filterOperator"></param>
    /// <param name="filterValue"></param>
    public FilterDescriptor(string propertyPath, FilterOperator filterOperator, object filterValue) {
      PropertyPath = propertyPath;
      Operator = filterOperator;
      Value = new Parameter("Value", filterValue);
      IgnoredValue = string.Empty;
      IsCaseSensitive = false;
      //Value.ParameterName = "Value";
      //Value.Value = filterValue;
    }

    /// <summary>
    /// Initialize parameters.
    /// </summary>
    /// <param name="elementContext"></param>
    /// <param name="culture"></param>
    public void Initialize(FrameworkElement elementContext, CultureInfo culture) {
      Value.Initialize(elementContext, culture);
    }

    /// <summary>
    /// Indicates if the filter should be ignored.
    /// </summary>
    public bool IsFilterIgnored {
      get {
        return object.Equals(Value.Value, IgnoredValue);
      }
    }

    /// <summary>
    /// The filter will be ignored when <see cref="Value"/> is equal to the ignored value.
    /// </summary>
    /// <remarks>
    /// The default value is an empty string.  This means that the filter will be ignored,
    /// and not used to filter the collection, when the <see cref="Value"/> is an empty string.
    /// Set the <b>IgnoredValue</b> as needed by your implementation.
    /// </remarks>
    public object IgnoredValue {
      get {
        return _ignoredValue;
      }
      set {
        if (_ignoredValue != value) {
          _ignoredValue = value;
          OnPropertyChanged("IgnoredValue");
        }
      }
    }

    /// <summary>
    /// Whether filter comparisons are case sensitive.
    /// </summary>
    /// <remarks>
    /// By default, filters are not case sensitive.
    /// </remarks>
    public bool IsCaseSensitive {
      get {
        return _isCaseSensitive;
      }
      set {
        if (_isCaseSensitive != value) {
          _isCaseSensitive = value;
          OnPropertyChanged("IsCaseSensitive");
        }
      }
    }

    /// <summary>
    /// The path to the property used for filtering.
    /// </summary>
    public string PropertyPath {
      get {
        return _propertyPath;
      }
      set {
        if (_propertyPath != value) {
          _propertyPath = value;
          OnPropertyChanged("PropertyPath");
        }
      }
    }

    /// <summary>
    /// The filter operator.
    /// </summary>
    public FilterOperator Operator {
      get {
        return _operator;
      }
      set {
        if (_operator != value) {
          _operator = value;
          OnPropertyChanged("Operator");
        }
      }
    }

    /// <summary>
    /// The filter value.
    /// </summary>
    /// <remarks>
    /// A <see cref="ControlParameter"/> can be used to reference a UI control providing
    /// the value.
    /// </remarks>
    public Parameter Value {
      get {
        return _value;
      }
      set {
        if (_value != value) {
          if (_value != null) {
            _value.PropertyChanged -= new PropertyChangedEventHandler(Value_PropertyChanged);
          }
          _value = value;
          if (_value != null) {
            _value.PropertyChanged += new PropertyChangedEventHandler(Value_PropertyChanged);
          }
          OnPropertyChanged("Value");
        }
      }
    }

    /// <summary>
    /// Returns a <see cref="PredicateDescription"/> built from this filter.
    /// </summary>
    /// <typeparam name="T">The type of the elements to be filtered</typeparam>
    /// <returns></returns>
    public PredicateDescription ToPredicateDescription<T>() {
      return ToPredicateDescription(typeof(T));
    }

    /// <summary>
    /// Returns a <see cref="PredicateDescription"/> built from this filter.
    /// </summary>
    /// <param name="elementType">The type of the elements to be filtered</param>
    /// <returns></returns>
    /// <remarks>
    /// Returns null if <see cref="IsFilterIgnored"/> is true.
    /// </remarks>
    public PredicateDescription ToPredicateDescription(Type elementType) {
      if (!this.IsFilterIgnored) {
        return new PredicateDescription(elementType, this.PropertyPath, this.Operator, this.Value.Value, !this.IsCaseSensitive);
      } else {
        return null;
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

    private void Value_PropertyChanged(object sender, PropertyChangedEventArgs e) {
      if (e.PropertyName == "Value") {
        OnPropertyChanged("Value");
      }
    }

    private bool _isCaseSensitive;
    private object _ignoredValue;
    private string _propertyPath;
    private FilterOperator _operator;
    private Parameter _value;

  }

}