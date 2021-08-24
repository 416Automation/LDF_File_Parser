using System.Windows;
using System.Windows.Controls;

namespace LDF_File_Viewer
{

    /// <summary>
    /// Interaction logic for LinViewer.xaml
    /// </summary>
    public partial class LinViewer : UserControl
    {
        public LinViewerViewModel _viewModel;
        public LinViewer()
        {
            InitializeComponent();
            _viewModel = new LinViewerViewModel();
            DataContext = _viewModel;
        }

        private void TextBox_PreviewDrop(object sender, System.Windows.DragEventArgs e)
        {
            e.Handled = true;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
                e.Effects = DragDropEffects.None;
        }

        private void Button_DragLeave(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length != 0)
            {
                // TODO add validations and logs
                var test = files[0];
                _viewModel.BrowseFile(test);
            }
        }
    }
}
