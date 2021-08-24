using GalaSoft.MvvmLight.Command;
using LDF_File_Parser.Logger;
using LDF_FILEPARSER;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace LDF_File_Viewer
{
    public class LinViewerViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Frame> _frames;
        private LinFileContents _linFileContent;
        private Frame _selectedFrame;

        public RelayCommand<string> Browse { get; set; }
        public ObservableCollection<Frame> Frames
        {
            get => _frames; set
            {
                if (_frames != value)
                {
                    _frames = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public LinFileContents LinFileContent
        {
            get => _linFileContent;
            set
            {
                if (_linFileContent != value)
                {
                    _linFileContent = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public Frame SelectedFrame
        {
            get => _selectedFrame; set
            {
                if (_selectedFrame != value)
                {
                    _selectedFrame = value;
                    NotifyPropertyChanged();

                }
            }
        }

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
                Logger.LogError(exc);
                MessageBox.Show(exc.ToString(), "File loading error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // This method is called by the Set accessors of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


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
            _selectedFrame.ConvertToByteArray();
        }
    }
}
