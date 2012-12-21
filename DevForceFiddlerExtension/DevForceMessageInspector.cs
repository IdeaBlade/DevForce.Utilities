using System;
using System.IO;
using System.ServiceModel.Channels;
using System.Windows.Forms;
using System.Xml;
using Fiddler;

[assembly: Fiddler.RequiredVersion("2.2.0.0")]      // I have no idea what this should be


namespace DevForceFiddlerExtension {

  abstract public class DevForceMessageInspector : Inspector2 {

    public DevForceMessageInspector() {
      Output = new TextView();
    }

    protected TextView Output { get; private set; }

    public string ContentType { get; set; }

    private const string ExpectedContentType = "application/x-gzip";

    public override void AddToTab(System.Windows.Forms.TabPage o) {
      o.Text = "DevForce";
      o.Controls.Add(Output);
      o.Controls[0].Dock = DockStyle.Fill;
    }

    // I can't figure out when this is called, and it doesn'tseem to do anything ...
    public override int ScoreForContentType(string sMIMEType) {
      if (sMIMEType.StartsWith(ExpectedContentType, StringComparison.OrdinalIgnoreCase)) {
        return 0x37;
      }
      return -1;
    }

    // This is apparently setting our tab order - a 0 appears to mean we don't care ...
    public override int GetOrder() {
      return 0;
    }

    protected void ClearOutput() {
      Output.SetText(string.Empty);
    }

    protected void DisplayMessage(byte[] bytes) {

      if (string.Compare(ContentType, ExpectedContentType, true) != 0) {
        Output.SetText("Content type not supported");
        return;
      }

      try {
        // Use Fiddler's gzip support to uncompress.
        var uncompressedBytes = Fiddler.Utilities.GzipExpand(bytes);
        // Now use WCF encoder to decipher (doing this means we don't have to provide the xml dictionary it wants)
        CreateAndWriteMessage(uncompressedBytes);
      } catch (Exception ex) {
        Output.SetText(ex.Message);       // Do this for debugging
      }
    }

    private void CreateAndWriteMessage(byte[] bytes) {
      using (var stream = new MemoryStream(bytes, false)) {
        var mebe = new BinaryMessageEncodingBindingElement();
        mebe.MessageVersion = MessageVersion.Soap12;
        mebe.ReaderQuotas.MaxArrayLength = XmlDictionaryReaderQuotas.Max.MaxArrayLength;
        mebe.ReaderQuotas.MaxDepth = XmlDictionaryReaderQuotas.Max.MaxDepth;
        mebe.ReaderQuotas.MaxStringContentLength = XmlDictionaryReaderQuotas.Max.MaxStringContentLength;
        var factory = mebe.CreateMessageEncoderFactory();
        var msg = factory.Encoder.ReadMessage(stream, 1024 * 16);     // I have no idea what header size to give it ...
        WriteMessage(msg);
      }
    }

    private void WriteMessage(System.ServiceModel.Channels.Message message) {
      using (var stringWriter = new StringWriter()) {
        using (var xmlWriter = new XmlTextWriter(stringWriter)) {
          xmlWriter.Formatting = Formatting.Indented;
          message.WriteMessage(xmlWriter);
          Output.SetText(stringWriter.ToString());
        }
      }
    }

  }
}
