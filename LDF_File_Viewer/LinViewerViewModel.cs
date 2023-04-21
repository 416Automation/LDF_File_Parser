using GalaSoft.MvvmLight.Command;
using LDF_File_Parser.Logger;
using LDF_FILEPARSER;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;

namespace LDF_File_Viewer
{
    public class LinViewerViewModel : INotifyPropertyChanged
    {
        public RelayCommand<string> Browse { get; set; }
        public ObservableCollection<Frame> Frames { get; set; }
        public LinFileContents LinFileContent { get; set; }
        public Frame SelectedFrame { get; set; }
        public RelayCommand<Signal> SelectedSignal { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinViewerViewModel"/> class.
        /// Constructor
        /// </summary>
        public LinViewerViewModel()
        {
            Browse = new RelayCommand<string>(BrowseFile);
            SelectedSignal = new RelayCommand<Signal>(SelectedSignalTest);
        }

      
        public event PropertyChangedEventHandler PropertyChanged;

        public void BrowseFile(string fileName = "")
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog { Filter = "LIN Description File|*.ldf" };

                    if (openFileDlg.ShowDialog() == true)
                        fileName = openFileDlg.FileName;
                }

                LinFileContent = new LinFileContents(fileName);
                Frames = new ObservableCollection<Frame>(LinFileContent.Frames);
                SelectedFrame = Frames[0];
            }
            catch (Exception exc)
            {
                if (Debugger.IsAttached) Debugger.Break();
                Logger.LogError(exc);
                MessageBox.Show(exc.ToString(), "File loading error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // This method is called by the Set accessors of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void SelectedSignalTest(Signal signal)
        {
            if (signal is null)
                throw new ArgumentNullException(nameof(signal));

            SignalValueUserControl signalValueUserControl = new SignalValueUserControl(signal);
            Window window = new Window()
            {
                Title = "Signal Valuer Selector",
                Content = signalValueUserControl,
                SizeToContent = SizeToContent.WidthAndHeight,
            };

            signalValueUserControl.Window = window;
            window.ShowDialog();
            SelectedFrame.ConvertToByteArray();
        }
    }
}
