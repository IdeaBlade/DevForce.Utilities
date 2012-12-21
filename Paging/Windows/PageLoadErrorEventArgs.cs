using IdeaBlade.EntityModel;
using System;

namespace IdeaBlade.Windows {

  /// <summary>
  /// Event arguments to the EntityqueryPagedCollectionView PageLoadError event.
  /// </summary>
  public class PageLoadErrorEventArgs : EventArgs {

    internal PageLoadErrorEventArgs(Exception error)  {
      Error = error;
    }

    public Exception Error {
      get;
      private set;
    }

    public bool IsErrorHandled {
      get;
      private set;
    }

    public void MarkErrorAsHandled() {
      IsErrorHandled = true;
    }
  }
}
