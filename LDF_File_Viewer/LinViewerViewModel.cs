using GalaSoft.MvvmLight.Command;
using LDF_FILEPARSER;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LDF_File_Viewer
{
    public class LinViewerViewModel : INotifyPropertyChanged
    {
        private LinFileContents _linFileContent;
        private ObservableCollection<Frame> _frames;
        private Frame _selectedFrame;

        public RelayCommand Browse { get; set; }
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
        public LinViewerViewModel()
        {
            Browse = new RelayCommand(BrowseFile);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void BrowseFile()
        {
            try
            {
                Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog { Filter = "LIN Description File|*.ldf" };

                if (openFileDlg.ShowDialog() == true)
                {
                    string fileNameWithPath = openFileDlg.FileName;
                    LinFileContent = new LinFileContents(fileNameWithPath);
                    Frames = new ObservableCollection<Frame>(LinFileContent.Frames);
                }
            }
            catch (Exception exc)
            {

                // TODO Handle the exceptions well

            }
        }



        // This method is called by the Set accessors of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
