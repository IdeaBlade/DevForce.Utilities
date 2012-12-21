using System.Windows.Controls;


namespace DevForceSilverlightApp {

  public partial class MainPage : UserControl {

    public MainPage() {
      InitializeComponent();
      Loaded += (s, e) => DataContext = new MainPageViewModel();
    }
  }
}
