using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LDF_FILEPARSER
{
    public class BoolValues : INotifyPropertyChanged
    {
        private bool _valueBool = false;

        public bool Enabled { get; set; } = false;

        public bool InUse { get; set; } = true;

        public int Placeholder { get; set; }

        public bool Value
        {
            get => _valueBool;
            set
            {
                if (_valueBool == value) return;
                {
                    _valueBool = value;
                    PerformBooleanToInt?.Invoke(this, null);
                }
            }
        }

        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event EventHandler PerformBooleanToInt;
        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString() => $"Placeholder: {Placeholder}, Enabled: {Enabled}";

        public BoolValues(int placeholder, bool enabled = false, bool value = false)
        {
            Placeholder = placeholder;
            Enabled     = enabled;
            Value       = value;
        }
    }
}