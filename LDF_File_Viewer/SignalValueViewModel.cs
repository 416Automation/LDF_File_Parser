using GalaSoft.MvvmLight.Command;
using LDF_FILEPARSER;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LDF_File_Viewer
{

    public class SignalValueViewModel : INotifyPropertyChanged
    {
        private IEncodingValue _selectedEncoding;

        public RelayCommand Cancel { get; set; }
        public bool EncodingsExisits { get; set; }
        public string ErrorMessage { get; set; }
        public IEncodingValue SelectedEncoding
        {
            get => _selectedEncoding; 
            set
            {
                if (_selectedEncoding == value) return;
                _selectedEncoding = value;
                Signal.HexValue = _selectedEncoding.HexAddress;
                Signal.IntegerValue = Convert.ToInt32(Signal.HexValue, 16);
            }
        }
        public Signal Signal { get; private set; }
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
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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
