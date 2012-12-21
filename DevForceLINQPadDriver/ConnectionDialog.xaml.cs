/*
/// <changelog>
///   <item version="1.0.0" when="Jan-2012">Initial version</item>
///   <item version="1.0.2" when="Jan-2012">Refactored for SQL logging;</item>
/// </changelog>
*/

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using LINQPad.Extensibility.DataContext;

namespace IdeaBlade.Drivers {
  /// <summary>
  /// Interaction logic for ConnectionDialog.xaml
  /// </summary>
  public partial class ConnectionDialog : Window {
    ConnectionProperties _properties;

    public ConnectionDialog(IConnectionInfo cxInfo) {
      DataContext = _properties = new ConnectionProperties(cxInfo);
      InitializeComponent();
    }

    void btnOK_Click(object sender, RoutedEventArgs e) {
      DialogResult = true;
    }

    void BrowseAssembly(object sender, RoutedEventArgs e) {
      var dialog = new Microsoft.Win32.OpenFileDialog() {
        Title = "Choose .NET model assembly",
        DefaultExt = ".dll",
      };

      if (dialog.ShowDialog() == true) {
        _properties.CustomAssemblyPath = dialog.FileName;
      }
    }

    void BrowseAppConfig(object sender, RoutedEventArgs e) {
      var dialog = new Microsoft.Win32.OpenFileDialog() {
        Title = "Choose application config file",
        DefaultExt = ".config",
      };

      if (dialog.ShowDialog() == true)
        _properties.AppConfigPath = dialog.FileName;
    }

    void ChooseType(object sender, RoutedEventArgs e) {
      string assemPath = _properties.CustomAssemblyPath;
      if (assemPath.Length == 0) {
        MessageBox.Show("First enter a path to an assembly.");
        return;
      }

      if (!File.Exists(assemPath)) {
        MessageBox.Show("File '" + assemPath + "' does not exist.");
        return;
      }

      string[] customTypes = _properties.GetEMTypes();
      if (customTypes.Length == 0) {
        MessageBox.Show("An EntityManager was not found in that assembly.");  
        return;
      }

      string result = (string)LINQPad.Extensibility.DataContext.UI.Dialogs.PickFromList("Choose EntityManager", customTypes);
      if (result != null) _properties.CustomTypeName = result;
    }

  }

  class ConnectionProperties : INotifyPropertyChanged {

    readonly IConnectionInfo _cxInfo;
    readonly XElement _driverData;

    public ConnectionProperties(IConnectionInfo cxInfo) {
      _cxInfo = cxInfo;
      _driverData = cxInfo.DriverData;
    }

    public event PropertyChangedEventHandler PropertyChanged = delegate { };
    
    public string CustomAssemblyPath {
      get { return _cxInfo.CustomTypeInfo.CustomAssemblyPath; }
      set {
        _cxInfo.CustomTypeInfo.CustomAssemblyPath = value;
        OnPropertyChanged("CustomAssemblyPath");
        CustomTypeName = GetEMTypes().FirstOrDefault();
        AppConfigPath = FindConfig();
      }
    }

    public string CustomTypeName {
      get { return _cxInfo.CustomTypeInfo.CustomTypeName; }
      set { 
        _cxInfo.CustomTypeInfo.CustomTypeName = value;
        OnPropertyChanged("CustomTypeName");
      }
    }

    public string AppConfigPath {
      get { return _cxInfo.AppConfigPath; }
      set { 
        _cxInfo.AppConfigPath = value;
        OnPropertyChanged("AppConfigPath");
      }
    }

    public bool Persist {
      get { return _cxInfo.Persist; }
      set { 
        _cxInfo.Persist = value;
        OnPropertyChanged("Persist");
      }
    }

    public bool LogSql {
      get {
        var el = _cxInfo.DriverData.Element("LogSql");
        return el == null ? false : System.Convert.ToBoolean(el.Value);
      }
      set { 
        _driverData.SetElementValue("LogSql", value);
        OnPropertyChanged("LogSql");
      }
    }

    public string[] GetEMTypes() {
      try {
        return _cxInfo.CustomTypeInfo.GetCustomTypesInAssembly("IdeaBlade.EntityModel.EntityManager");
      } catch (Exception) {
        return new string[0];
      }
    }

    private string FindConfig() {
      // Look for a config in same folder as assembly
      var folder = Path.GetDirectoryName(_cxInfo.CustomTypeInfo.CustomAssemblyPath);
      return Directory.GetFiles(folder, "*.config").FirstOrDefault();
    }

    private void OnPropertyChanged(string name) {
      PropertyChanged(this, new PropertyChangedEventArgs(name));
    }

  }

}
