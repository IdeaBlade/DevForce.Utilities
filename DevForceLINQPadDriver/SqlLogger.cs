/*
/// <changelog>
///   <item version="1.0.2" when="Jan-2012">Show generated SQL</item>
/// </changelog>
*/
using System;
using System.Reflection;
using System.Threading;

namespace IdeaBlade.Drivers {
  /// <summary>
  /// Utility class which will obtain the SQL for a query.
  /// DevForce does not generate SQL: it allows EF to do this, on the server, wwhen a DevForce query is reshaped as an ObjectQuery.
  /// Because of this, DevForce never really has the generated SQL, it must ask EF for it.  To obtain the SQL here
  /// we're turning on a DevForce logging attribute, "ShouldLogSqlQueries", to allow SQL messages to be written by the DevForce
  /// trace publisher.  We're then subscribing to the publisher and looking only for messages coming from a specific source.
  /// The publishing mechanism within DevForce runs on a separate thread, and pushes out all trace/debug messages.
  /// LINQPad needs the SQL for display purposes by the time the query has finished executing - which is a problem for us
  /// because the trace message may not have been published or received yet.  To work around this, the driver will call here
  /// to pull the SQL message, with a short wait if necessary, instead of something more intuitive like using an event handler.
  /// </summary>
  internal class SqlLogger {

    public SqlLogger() {
      Initialize();
    }

    public void Reset() {
      _resetEvent.Reset();
      _lastSQL = string.Empty;
    }

    private void Initialize() {
      try {
        // Set IdeaBladeConfig.Instance.Logging.ShouldLogSqlQueries to true.
        var ctype = DevForceTypes.Core.GetType("IdeaBlade.Core.IdeaBladeConfig");
        var pi2 = ctype.GetProperty("Instance", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Static);
        dynamic config = pi2.GetValue(null, null);
        config.Logging.ShouldLogSqlQueries = true;

        // And subscribe to trace publishing.
        var tsType = DevForceTypes.Core.GetType("IdeaBlade.Core.TraceSubscriber");
        dynamic subscriber = Activator.CreateInstance(tsType);
        var ev = tsType.GetEvent("Publish");

        var miHandler = this.GetType().GetMethod("PublishHandler", BindingFlags.NonPublic | BindingFlags.Instance);
        ev.AddEventHandler(subscriber, Delegate.CreateDelegate(ev.EventHandlerType, this, miHandler));
        subscriber.StartSubscription();
      } catch (Exception ex) {
        // eat - we just won't provide SQL info for the query.
      }
    }

    private void PublishHandler(object sender, EventArgs e) {
      // Find sql logging messages. 
      // Ths handler will actually be called on a separate thread, which we take advantage of here to signal the main
      // thread that we've received a SQL message.
      dynamic pe = e;
      var msg = pe.TraceMessage;
      if (msg.Source == "IdeaBlade.EntityModel.Edm.ObjectQueryProcessor:WriteGeneratedSql") {
        _lastSQL = msg.Message;
        _resetEvent.Set();
      }
    }

    /// <summary>
    /// This will wait for up to 1 full second to obtain a logged SQL message (if not already received).
    /// </summary>
    public string GetMessageWithWait() {
      _resetEvent.WaitOne(1000);
      return _lastSQL;
    }


    private AutoResetEvent _resetEvent = new AutoResetEvent(false);
    private string _lastSQL;

  }
}
