using System.Windows.Controls;

namespace LDF_File_Viewer
{

    /// <summary>
    /// Interaction logic for LinViewer.xaml
    /// </summary>
    public partial class LinViewer : UserControl
    {
        public LinViewer()
        {
            InitializeComponent();
            var viewModel = new LinViewerViewModel();
            DataContext = viewModel;
        }
    }
}
