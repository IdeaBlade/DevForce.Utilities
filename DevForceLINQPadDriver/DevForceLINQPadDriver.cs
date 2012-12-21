/*
/// <changelog>
///   <item version="1.0.0" when="Jan-2012">Initial version</item>
///   <item version="1.0.1" when="Jan-2012">Code First support</item>
///   <item version="1.0.2" when="Jan-2012">Add IdeaBlade.Linq; complex types; sql logging; refactoring</item>
///   <item version="1.0.4" when="Dec-2012">Removed _enableCopyConstructor support</item>
/// </changelog>
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LINQPad.Extensibility.DataContext;

namespace IdeaBlade.Drivers {

  /// <summary>
  /// A LINQPad driver for DevForce entity models.  This driver is based on the UniversalStaticDriver sample from LINQPad.
  /// 
  /// LINQPad will new a "context", your EntityManager, each time a query is run.  It will also use (at least) one AppDomain
  /// per query tab. You'll sometimes see a new AppDomain has been created when you least expect it - so watch out.
  /// It' important to not lock the model assembly - one of the nice features of LINQPad is that you can change your model
  /// and rebuild without the assembly being locked, and LINQPad will also (eventually) refresh to show your model changes.
  /// 
  /// This driver does not have any static references to DevForce assemblies - but it does use reflection to load them.  
  /// </summary>
  public class DevForceLINQPadDriver : StaticDataContextDriver {

    public override string Name { get { return "DevForce LINQPad Driver"; } }

    public override string Author { get { return "IdeaBlade"; } }

    public override string GetConnectionDescription(IConnectionInfo cxInfo) {
      // Save the cxInfo to use elsewhere.  Note this method is called a lot, but it seems to be the first time we'll see the cxInfo.
      _cxInfo = cxInfo;

      // We show the namespace qualified typename.
      return cxInfo.CustomTypeInfo.CustomTypeName;
    }

    public override bool AreRepositoriesEquivalent(IConnectionInfo c1, IConnectionInfo c2) {
      if (c1 == null && c2 == null) return true;
      if (c1 == null || c2 == null) return false;
      return string.Compare(c1.CustomTypeInfo.CustomTypeName, c2.CustomTypeInfo.CustomTypeName, true) == 0;
    }

    /// <summary>
    /// Add DevForce assemblies.
    /// </summary>
    public override IEnumerable<string> GetAssembliesToAdd() {
      return DevForceTypes.GetAssembliesToAdd(_cxInfo.CustomTypeInfo.CustomAssemblyPath);
    }

    /// <summary>
    /// Add DevForce namespaces.
    /// </summary>
    public override IEnumerable<string> GetNamespacesToAdd() {
      return new string[] { "IdeaBlade.EntityModel", "IdeaBlade.Core", "IdeaBlade.Linq" };
    }

    /// <summary>
    /// Remove the default Linq to SQL namespaces.
    /// </summary>
    public override IEnumerable<string> GetNamespacesToRemove() {
      return new string[] { "System.Data.Linq", "System.Data.Linq.SqlClient" };
    }

    /// <summary>
    /// Use the default parameterless EntityManager constructor.
    /// After construction we can set some EM properties in the InitializeContext method.  
    /// </summary>
    public override ParameterDescriptor[] GetContextConstructorParameters(IConnectionInfo cxInfo) {
      return null;
    }

    /// <summary>
    /// We're using the parameterless EM constructor, so no constructor arguments are provided.
    /// </summary>
    public override object[] GetContextConstructorArguments(IConnectionInfo cxInfo) {

      // We need to fix MEF probing in some circumstances, so let's check and do it before creating the EM.  
      DevForceTypes.CheckComposition(cxInfo);

      return null;
    }

    /// <summary>
    /// This is called after the EM is constructed.    
    /// </summary>
    public override void InitializeContext(IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager) {

      // Set properties on the EM now
      DevForceTypes.InitializeEntityManager(context);

      // Initialize sql logging if enabled.
      InitializeLogger(cxInfo);
    }

    /// <summary>
    /// To avoid walking the entire entity graph we use a custom provider.
    /// </summary>
    public override LINQPad.ICustomMemberProvider GetCustomDisplayMemberProvider(object objectToWrite) {
      if (objectToWrite != null && EntityMemberProvider.IsEntityOrComplexType(objectToWrite.GetType())) {
        return new EntityMemberProvider(objectToWrite);
      } else {
        return null;
      }
    }

    /// <summary>
    /// Populate SQL tab if sql logging is wanted.
    /// </summary>
    public override void OnQueryFinishing(IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager) {
      // LINQPad populates the sql tab just after this method is called, so this is our last chance to grab the sql.
      // The stopwatch is also stopped after this method completes, so any wait here counts in total exec time shown for the query.
      // The problem is that we're using a trace listener to obtain the sql, and it won't necessarily have the sql message
      // by the time this is called.  If we have it fire an event to tell us when the sql mesage has arrived it will be too late.
      // So, we ask the logger for it, and the logger will impose a short wait if it's not ready.  If the wait period
      // elapses without having obtained the sql, then we don't have anything to show in the sql tab for this execution.

      if (Logging) {
        executionManager.SqlTranslationWriter.Write(_logger.GetMessageWithWait());
      }
    }

    /// <summary>
    /// This opens the standard connection dialog (heavily based on the Universal demo driver).
    /// </summary>
    public override bool ShowConnectionDialog(IConnectionInfo cxInfo, bool isNewConnection) {
      return new ConnectionDialog(cxInfo).ShowDialog() == true;
    }

    /// <summary>
    /// Returns the schema for the EntityManager, showing EntityQueries and sprocs.
    /// </summary>
    public override List<ExplorerItem> GetSchema(IConnectionInfo cxInfo, Type customType) {
      return SchemaHelper.GetSchema(customType);
    }

    private bool EnableSqlLogging(IConnectionInfo cxinfo) {
      var el = cxinfo.DriverData.Element("LogSql");
      return el == null ? false : Convert.ToBoolean(el.Value);
    }

    private bool Logging {
      get { return _logger != null; }
    }

    private void InitializeLogger(IConnectionInfo cxinfo) {
      if (!EnableSqlLogging(cxinfo)) return;

      if (_logger == null) {
        _logger = new SqlLogger();
      }
      _logger.Reset();
    }

    private IConnectionInfo _cxInfo; // Careful - this can sometimes be null 
    private static SqlLogger _logger;

  }
}
