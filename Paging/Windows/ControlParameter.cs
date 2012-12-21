using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace IdeaBlade.Windows {


  /// <summary>
  /// A <see cref="Parameter"/> which references a UI control.
  /// </summary>
  public class ControlParameter : Parameter, INotifyPropertyChanged {

    /// <summary>
    /// Create an instance of this class.
    /// </summary>
    public ControlParameter()
      : base() {
    }

    /// <summary>
    /// Initialize the parameter in the UI.
    /// </summary>
    /// <param name="elementContext"></param>
    /// <param name="culture"></param>
    public override void Initialize(FrameworkElement elementContext, CultureInfo culture) {
      _elementContext = elementContext;
      _culture = culture;
      InitializeCore();
    }

    private void InitializeCore() {

      UnregisterRefreshEvent();

      _control = ValidateControlName(ControlName);
      _propertyInfo = ValidatePropertyName(PropertyName);
      _eventInfo = ValidateEventName(RefreshEventName);

      SetupHandler();
    }

    private void SetupHandler() {
      //UnregisterRefreshEvent();
      RegisterRefreshEvent();
      SetValueFromControl();
    }

    /// <summary>
    /// The name of the UI control.
    /// </summary>
    public string ControlName {
      get {
        return _controlName;
      }
      set {
        if (_controlName != value) {
          _controlName = value;
          InitializeCore();
          OnPropertyChanged("ControlName");
        }
      }
    }

    /// <summary>
    /// The property on the control which provides the parameter value.
    /// </summary>
    public string PropertyName {
      get {
        return _propertyName;
      }
      set {
        if (_propertyName != value) {
          var pi = ValidatePropertyName(value);
          _propertyName = value;
          _propertyInfo = pi;
          SetValueFromControl();
          OnPropertyChanged("PropertyName");
        }
      }
    }

    /// <summary>
    /// The name of the control event to listen for which triggers a refresh.
    /// </summary>
    /// <remarks>
    /// For example, "TextChanged" or "SelectionChanged" might be used to 
    /// trigger a refresh of the displayed data.
    /// </remarks>
    public string RefreshEventName {
      get {
        return _refreshEventName;
      }
      set {
        if (_refreshEventName != value) {
          var ei = ValidateEventName(value);
          UnregisterRefreshEvent();
          _refreshEventName = value;
          _eventInfo = ei;
          RegisterRefreshEvent();
          OnPropertyChanged("RefreshEventName");
        }
      }
    }

    //public override object Value {
    //  get {
    //    if (!SetupHandler(false)) {
    //      SetValueFromControl();
    //    }
    //    return base.Value;
    //  }
    //  set {
    //    base.Value = value;
    //  }
    //}


    private UIElement ValidateControlName(string name) {
      if (_elementContext == null) { return null; }

      if (string.IsNullOrEmpty(name)) {
        throw new ArgumentNullException("ControlName not specified");
      }

      var control = FindControl(_elementContext, name);
      if (control == null) {
        throw new ArgumentException(string.Format("Control '{0}' not found.", name));
      }
      return control;
    }

    private PropertyInfo ValidatePropertyName(string name) {
      if (_control == null) { return null; }
      return GetProperty(GetResolvedPropertyName(name));
    }

    private EventInfo ValidateEventName(string name) {
      if (_control == null) { return null; }

      Type type = _control.GetType();
      var eventInfo = type.GetEvent(GetResolvedEventName(name));
      if (eventInfo == null) {
        throw new ArgumentException(string.Format("Event '{0}' not found on control '{1}'", name, ControlName));
      }
      return eventInfo;
    }

    private string GetResolvedPropertyName(string propertyName) {

      if (!string.IsNullOrEmpty(propertyName)) { return propertyName; }

      if (_control is TextBox) {
        return "Text";
      }
      if (_control is ToggleButton) {
        return "IsChecked";
      }

      throw new ArgumentNullException("PropertyName not specified");
    }

    private string GetResolvedEventName(string eventName) {

      if (!string.IsNullOrEmpty(eventName)) { return eventName; }

      if (_control is TextBox) {
        return "TextChanged";
      }
      if (_control is ToggleButton) {
        return "Checked";
      }

      throw new ArgumentNullException("RefreshEventName not specified");
    }


    private PropertyInfo GetProperty(string propertyName) {
      // RIA supports nested properties, so assume we need to also.

      Type type = _control.GetType();
      PropertyInfo pi = null;

      foreach (string part in propertyName.Split('.')) {
        pi = type.GetProperty(part);
        if (pi == null) {
          throw new ArgumentException(string.Format("Property '{0}' not found on control '{1}'", propertyName, ControlName));
        }
        type = pi.PropertyType;
      }

      return pi;
    }



    //public override object GetConvertedValue(Type convertedValueType) {
    //  if ((convertedValueType != null) && (Converter != null)) {
    //    object obj2 = Converter.Convert(Value, convertedValueType, ConverterParameter, _culture);
    //    if ((obj2 != null) && convertedValueType.IsAssignableFrom(obj2.GetType())) {
    //      return obj2;
    //    }
    //  }
    //  return base.GetConvertedValue(convertedValueType);
    //}


    /// <summary>
    /// 
    /// </summary>
    /// <param name="propertyName"></param>
    protected override void OnPropertyChanged(string propertyName) {
      base.OnPropertyChanged(propertyName);
      if (propertyName == "Value") {
        // this wants to push value into the control.... i'm not sure why
        //PopulateControlProperty();
      }
    }

    //private void PopulateControlProperty() {
    //  if (_control != null) {
    //    string propertyNamePrivate = PropertyNamePrivate;
    //    if (!string.IsNullOrEmpty(propertyNamePrivate)) {
    //      TextBox box = _control as TextBox;
    //      if ((box != null) && (propertyNamePrivate == "Text")) {
    //        if ((box.SelectionLength == 0) && (box.SelectionStart == box.Text.Length)) {
    //          box.Text = base.Value as string;
    //          box.SelectionStart = box.Text.Length;
    //        } else {
    //          box.Text = base.Value as string;
    //        }
    //      } else {
    //        TypeHelper.SetNestedPropertyValue(_control, base.Value, propertyNamePrivate);
    //      }
    //    }
    //  }
    //}

    private void SetValueFromControl() {
      // Set the parameter value from the Control info

      if (_control == null || _propertyInfo == null) {
        Value = null;
      } else {
        Value = _propertyInfo.GetValue(_control, null);
      }
    }

    private static void HandleRefreshEvent(object sender, RoutedEventArgs e) {

      var element = sender as UIElement;

      RefreshEventInfo evinfo;
      if (__map.TryGetValue(element, out evinfo)) {
        evinfo.RefreshAction();
      }
    }


    private void UnregisterRefreshEvent() {
      if (_control == null || _eventInfo == null) return;

      _eventInfo.RemoveEventHandler(_control, GetHandlerDelegate());
      __map.Remove(_control);
    }


    private void RegisterRefreshEvent() {
      if (_control == null || _eventInfo == null) return;

      _eventInfo.AddEventHandler(_control, GetHandlerDelegate());

      // This is not a great way of tracking this, since you could conceivably have multiple
      // parameters on a page for the same control....
      RefreshEventInfo ei = new RefreshEventInfo() {
        Control = _control,
        EventName = RefreshEventName,
        RefreshAction = () => { SetValueFromControl(); }
      };

      __map.Add(_control, ei);
    }

    private Delegate GetHandlerDelegate() {
      // Method has to be static to get past problem of binding to a target not of the correct control type
      var mi = this.GetType().GetMethod("HandleRefreshEvent", BindingFlags.NonPublic | BindingFlags.Static);
      return Delegate.CreateDelegate(_eventInfo.EventHandlerType, mi);
    }

    private static FrameworkElement FindControl(FrameworkElement element, string name) {
      while (element != null) {
        FrameworkElement element2 = element.FindName(name) as FrameworkElement;
        if (element2 != null) {
          return element2;
        }
        element = VisualTreeHelper.GetParent(element) as FrameworkElement;
      }
      return null;
    }


    private static Dictionary<UIElement, RefreshEventInfo> __map = new Dictionary<UIElement, RefreshEventInfo>();

    private CultureInfo _culture;
    private FrameworkElement _elementContext;

    private string _controlName;
    private string _propertyName;
    private string _refreshEventName;

    private UIElement _control;
    private PropertyInfo _propertyInfo;
    private EventInfo _eventInfo;

    //private IValueConverter _converter;
    //private object _converterParameter;


    private struct RefreshEventInfo {
      public UIElement Control { get; set; }
      public string EventName { get; set; }
      public Action RefreshAction { get; set; }
    }

  }
}