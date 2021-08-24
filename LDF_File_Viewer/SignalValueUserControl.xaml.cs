using LDF_FILEPARSER;
using System.Windows;
using System.Windows.Controls;

namespace LDF_File_Viewer
{

    /// <summary>
    /// Interaction logic for SignalValueUserControl.xaml
    /// </summary>
    public partial class SignalValueUserControl : UserControl
    {
        public Window Window { get; set; }
        public SignalValueUserControl(Signal signal)
        {
            InitializeComponent();
            SignalValueViewModel signalValueViewModel = new SignalValueViewModel(signal);
            DataContext = signalValueViewModel;
            signalValueViewModel.Close += SignalValueViewModel_Close;
        }

        private void SignalValueViewModel_Close(object sender, System.EventArgs e)
        {
            Window?.Close();
        }
    }
}
