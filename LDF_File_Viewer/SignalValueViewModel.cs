using GalaSoft.MvvmLight.Command;
using LDF_FILEPARSER;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LDF_File_Viewer
{

    public class SignalValueViewModel : INotifyPropertyChanged
    {
        private bool _encodingsExisits;
        private string _errorMessage;
        private IEncodingValue _selectedEncoding;
        private Signal _signal;
        public RelayCommand Cancel { get; set; }

        public bool EncodingsExisits
        {
            get => _encodingsExisits; set
            {
                if (_encodingsExisits != value)
                {
                    _encodingsExisits = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage; set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public IEncodingValue SelectedEncoding
        {
            get => _selectedEncoding; set
            {
                if (_selectedEncoding != value)
                {
                    _selectedEncoding = value;
                    Signal.HexValue = _selectedEncoding.HexAddress;
                    Signal.IntegerValue = Convert.ToInt32(Signal.HexValue, 16);
                    NotifyPropertyChanged();
                }
            }
        }

        public Signal Signal
        {
            get => _signal; private set
            {
                if (_signal != value)
                {
                    _signal = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public RelayCommand Submit { get; set; }
        public SignalValueViewModel(Signal signal)
        {
            Signal = signal;
            Signal.IntegerValueOutOfRange += Signal_IntegerValueOutOfRange;
            EncodingsExisits = signal.Encoding != null;
            Submit = new RelayCommand(SubmitPressed);
            Cancel = new RelayCommand(CancelPressed);
        }

        public event EventHandler Close;

        public event PropertyChangedEventHandler PropertyChanged;

        private void CancelPressed()
        {
            if (Signal.ValueNotInRange is false)
            {
                Close?.Invoke(this, null);
            }
        }

        // This method is called by the Set accessors of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Signal_IntegerValueOutOfRange(object sender, int invalidInteger)
        {
            ErrorMessage = $"Integer out of range, Max range {Signal.MaxValue}, Value provided: {invalidInteger}";
        }
        private void SubmitPressed()
        {
            if (Signal.ValueNotInRange is false)
            {
                CancelPressed();
            }
        }
    }
}
