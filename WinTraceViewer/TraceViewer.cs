using IdeaBlade.Core;
using System;
using System.Windows.Forms;

namespace IdeaBlade.DevTools.TraceViewer {

  /// <summary>
  /// Utility which provides a real-time display of DevForce tracing messages.
  /// <seealso cref="T:IdeaBlade.Core.TraceFns"/>
  /// </summary>
  /// <remarks>
  /// You can run the <b>TraceViewer</b> as a standaone utility 
  /// to display tracing messages from a BOS or other application 
  /// by launching <b>WinTraceViewer.exe</b> from the command line.  
  /// You can also open the TraceViewer window directly from your application 
  /// to view it's tracing messages by constructing a
  /// new instance of the <see cref="TraceViewerForm"/> window and then calling 
  /// <see cref="System.Windows.Forms.Form.Show">Show</see> on it.
  /// <para>
  /// When invoked from the command line the TraceViewer utility will display
  /// tracing messages from a BOS or other running DevForce application.  The application
  /// must be "publishing" tracing messages.  If your BOS is hosted by either
  /// the ServerConsole or ServiceService then publishing is on by default.  If your BOS is
  /// hosted by IIS, or for a non-BOS application, you must enable publishing by calling 
  /// <see cref="IdeaBlade.Core.TracePublisher.MakeRemotable()">TracePublisher.LocalInstance.MakeRemotable()</see>
  /// in the startup logic (Application_Start in the global.asax for a BOS under IIS).
  /// </para>
  /// <para>
  /// The default URL which this utility connects to is usually "net.tcp://localhost:9922/TracePublisher".
  /// To override this, required when the application is running on another machine or using a non-default
  /// port, pass the URL as the command line argument:
  /// <code>WPFTraceViewer.exe "net.tcp://myserver:2299/TracePublisher"</code>
  /// Also see the <see cref="TracePublisher"/> topic for information on overriding publishing defaults.
  /// </para>
  /// <para>
  /// Note that the <b>WinTraceViewer</b> and the <b>WPFTraceViewer</b> perform the same
  /// function; one is a Windows Forms implementation and one is a WPF implementation,
  /// but either can be used in any environment and will display the same results.
  /// </para>
  /// </remarks>
  public class TraceViewer {

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(String[] pArgs) {

      // The TraceViewer always tries to connect to a remote TracePublisher - but
      // it gets the default URL from the local publisher.
      // String localURL = TracePublisher.CreateInstance(technology).GetPublisherUrl(null);
      String localURL = TracePublisher.LocalInstance.GetPublisherUrl(null);
      Application.Run(new TraceViewerForm(localURL));
    }
  }
}
