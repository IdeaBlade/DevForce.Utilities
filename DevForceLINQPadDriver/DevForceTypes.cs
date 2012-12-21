/*
/// <changelog>
///   <item version="1.0.1.0" when="Jan-2012">Initial version</item>
///   <item version="1.0.1.0" when="Jan-18-2012">Code First support</item>
///   <item version="1.0.2.0" when="Jan-2012">Support complex types; include IdeaBlade.Linq</item>
///   <item version="1.0.4.0" when="Dec-2012">Removed "DefaultEM"; allow default DevForce login to occur</item>
/// </changelog>
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LINQPad.Extensibility.DataContext;
using Microsoft.Win32;

namespace IdeaBlade.Drivers {

  /// <summary>
  /// This class provides access to DevForce types, objects, etc. using reflection and dynamic typing.
  /// LINQPad creates an AppDomain per query tab, but will (usually) reuse that AppDomain for other
  /// queries executed on that tab.  So statics can be useful, at least when the same query tab is reused.
  /// </summary>
  internal static class DevForceTypes {

    public static Assembly EntityModel {
      get {
        if (_ibEM == null) {
          _ibEM = LoadAssembly("IdeaBlade.EntityModel");

          if (_ibEM == null) {
            throw new NotSupportedException("Cannot find IdeaBlade.EntityModel");
          }
        }
        return _ibEM;
      }
    }

    public static Assembly Core {
      get {
        if (_ibCore == null) {
          _ibCore = LoadAssembly("IdeaBlade.Core");

          if (_ibCore == null) {
            throw new NotSupportedException("Cannot find IdeaBlade.Core");
          }
        }
        return _ibCore;
      }
    }

    public static IEnumerable<string> GetAssembliesToAdd(string customPath) {
      if (_assemblies == null) {

        // First look in folder holding the model, then in DevForce install folder.
        var folder = Path.GetDirectoryName(customPath);
        var asmFile = Path.Combine(folder, "Ideablade.EntityModel.dll");
        if (!File.Exists(asmFile)) {
          folder = GetInstallFolderFromRegistry();
        }
        _assemblies = new string[] { 
            System.IO.Path.Combine(folder, "IdeaBlade.Core.dll"), 
            System.IO.Path.Combine(folder, "IdeaBlade.Linq.dll"),
            System.IO.Path.Combine(folder, "IdeaBlade.Validation.dll"), 
            System.IO.Path.Combine(folder, "IdeaBlade.EntityModel.dll")
          };
      }
      return _assemblies;
    }

    /// <summary>
    /// Calls EntityMetadata.IsComplexType method.
    /// </summary>
    public static bool IsComplexType(Type t) {
      if (_isComplexType == null) {
        try {
          var emType = EntityModel.GetType("IdeaBlade.EntityModel.EntityMetadata");
          _isComplexType = emType.GetMethod("IsComplexType", BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static);
        } catch (Exception ex) {
          throw new ApplicationException("Cannot obtain EntityMetadata", ex);
        }
      }

      return (bool)_isComplexType.Invoke(null, new[] { t });
    }

    /// <summary>
    /// Ensure CompositionHost.SearchFolders has a valid item.
    /// When using a web.config, the list will contain a null item and cause DevForce to throw, so we reset with the bin folder.
    /// It's not clear how often this is needed - CompositionHost.SearchFolders is a static, yet 
    /// testing shows that this is often reset, so it's not really clear when LINQPad uses a new AppDomain.
    public static void CheckComposition(IConnectionInfo cxInfo) {
      var pi = SearchFolders;
      var propValue = pi.GetValue(null, null) as List<string>;
      if (propValue == null || propValue[0] == null) {
        var folder = Path.GetDirectoryName(cxInfo.CustomTypeInfo.CustomAssemblyPath);
        var newList = new List<string>();
        newList.Add(folder);
        pi.SetValue(null, newList, null);
      }
    }


    public static PropertyInfo SearchFolders {
      get {
        if (_searchFolders == null) {
          var ch = Core.GetType("IdeaBlade.Core.Composition.CompositionHost");
          _searchFolders = ch.GetProperty("SearchFolders", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Static);
        }
        return _searchFolders;
      }
    }

    /// <summary>
    /// Set a DataSourceOnly query strategy.  If non-credentialed login is not supported, you should modify this code to also login here.
    /// The DataSourceOnly query strategy is more efficient, since cache isn't helpful when LINQPad recreates the EM
    /// for each query.  
    /// </summary>
    /// <param name="entityManager"></param>
    public static void InitializeEntityManager(object entityManager) {
      dynamic em = entityManager;
      dynamic qs = QsDataSourceOnly;
      em.DefaultQueryStrategy = qs;
    }

    public static object QsDataSourceOnly {
      get {
        if (_qsDataSourceOnly == null) {
          try {
            var qsType = EntityModel.GetType("IdeaBlade.EntityModel.QueryStrategy");
            var fi = qsType.GetField("DataSourceOnly", BindingFlags.GetField | BindingFlags.Public | BindingFlags.Static);
            _qsDataSourceOnly = fi.GetValue(null);
          } catch (Exception ex) {
            throw new ApplicationException("Cannot obtain IdeaBlade.EntityModel.QueryStrategy.DataSourceOnly", ex);
          }
        }
        return _qsDataSourceOnly;
      }
    }

    public static object EntityMetadataStore {
      get {
        if (_entityMetadataStore == null) {
          try {
            var emsType = EntityModel.GetType("IdeaBlade.EntityModel.EntityMetadataStore");
            var pi = emsType.GetProperty("Instance", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Static);
            _entityMetadataStore = pi.GetValue(null, null);
          } catch (Exception ex) {
            throw new ApplicationException("Cannot obtain IdeaBlade.EntityModel.EntityMetadataStore.Instance", ex);
          }
        }
        return _entityMetadataStore;
      }
    }

    private static Assembly LoadAssembly(string name, bool throwIfNotFound = false) {
      try {
        var asm = Assembly.LoadWithPartialName(name);
        return asm;
      } catch (Exception) {
        if (throwIfNotFound) {
          throw;
        } else {
          return null;
        }
      }
    }

    private static string GetInstallFolderFromRegistry() {
      // For DF2010.  With DF2012, assemblies must always be in same folder as model.
      var registryKeyName = @"SOFTWARE\{0}Microsoft\.NETFramework\AssemblyFolders\IdeaBlade2010";
      string keyName = string.Format(registryKeyName, Environment.Is64BitOperatingSystem ? "Wow6432Node\\" : string.Empty);
      using (var key = Registry.LocalMachine.OpenSubKey(keyName)) {
        return key.GetValue(null) as string;
      }
    }

    private static Assembly _ibEM;
    private static Assembly _ibCore;
    private static object _entityMetadataStore;
    private static object _qsDataSourceOnly;
    private static IEnumerable<string> _assemblies;
    private static PropertyInfo _searchFolders;
    private static MethodInfo _isComplexType;

  }
}
