using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace DevForceSilverlightApp {

  /// <summary>
  /// Used to add a parameterless constructor for the EM.  The ODS used
  /// here defines its own EM, and since created in XAML a parameter-less constructor
  /// is required.
  /// 
  /// </summary>
  public partial class NorthwindIBEntities {

    public NorthwindIBEntities() : base() {

    }
  }
}
