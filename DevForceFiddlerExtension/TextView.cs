using System.Windows.Forms;

namespace DevForceFiddlerExtension
{
    public partial class TextView : UserControl
    {
        public TextView()
        {
            InitializeComponent();
        }

        public void SetText(string text)
        {
            _text.Text = text;
        }
    }
}
