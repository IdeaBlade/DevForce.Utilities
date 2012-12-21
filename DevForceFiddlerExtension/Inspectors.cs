using Fiddler;

namespace DevForceFiddlerExtension {

  public class RequestInspector : DevForceMessageInspector, IRequestInspector2 {

    public HTTPRequestHeaders headers {
      get {
        return null;      // This means we don't allow editing
      }
      set {
        ContentType = value["content-type"];
      }
    }

    public void Clear() {
      ClearOutput();
    }

    public bool bDirty {
      get { return false; }
    }

    public bool bReadOnly {
      get { return true; }
      set {}
    }

    public byte[] body {
      get {
        return null;  // Apparently this also means we don't allow editing ...
      }
      set {
        DisplayMessage(value);
      }
    }

  }

  public class ResponseInspector : DevForceMessageInspector, IResponseInspector2 {

    public HTTPResponseHeaders headers {
      get {
        return null; 
      }
      set {
        ContentType = value["content-type"];
      }
    }

    public void Clear() {
      ClearOutput();
    }

    public bool bDirty {
      get { return false; }
    }

    public bool bReadOnly {
      get { return true; }
      set {}
    }

    public byte[] body {
      get {
        return null;  // Apparently this also means we don't allow editing ...
      }
      set {
        DisplayMessage(value);
      }
    }

  }

}
