using GalaSoft.MvvmLight.Command;
using LDF_File_Parser.Extension;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LDF_FILEPARSER
{
    public class Signal : INotifyPropertyChanged
    {
        private string _hexValue = "0x00";
        private int _integerValue = 0;
        private IEncodingValue _selectedEncoding;

        public ICollection<BoolValues> BooleanValues { get; set; }
        public RelayCommand ClearAll { get; private set; }
        public EncodingNode Encoding { get; set; }

        public IEncodingValue SelectedEncoding
        {
            get => _selectedEncoding;
            set
            {
                if (_selectedEncoding == value) return;
                _selectedEncoding = value;
                HexValue = _selectedEncoding.HexAddress;
                IntegerValue = Convert.ToInt32(HexValue, 16);
            }
        }

        public Frame Frame { get; private set; }
        public string HexValue
        {
            get => _hexValue; 
            set
            {
                if (_hexValue == value) return;
                _hexValue = value;
                
                IntegerValue = Convert.ToInt32(_hexValue, 16);
                ConvertBooleanArray(IntegerValue);

                var foundEncoding = Encoding?.EncodingTypes.First(s => string.Equals(s.HexAddress, _hexValue));
                if (foundEncoding != null) SelectedEncoding = foundEncoding;
            }
        }

        public string InitalValue { get; private set; }
        public int IntegerValue
        {
            get => _integerValue; 
            set
            {
                if (_integerValue == value) return;
                if (value > MaxValue)
                {
                    IntegerValueOutOfRange?.Invoke(this, value);
                    _integerValue = 0;
                    ValueNotInRange = true;
                }
                else
                {
                    _integerValue = value;
                    ValueNotInRange = false;

                }

                HexValue = _integerValue.ToString().ConvertToHex();
                ConvertBooleanArray(_integerValue);
                Frame?.ConvertToByteArray();
            }
        }

        public string MaxHexValue => MaxValue.ToString().ConvertToHex();
        public int MaxValue => BooleanValues.Where(s => s.Enabled).Sum(k => (int)Math.Pow(2, k.Placeholder));
        public string Name { get; private set; }
        public string[] RawSignalValues { get; }
        public RelayCommand SelectAll { get; set; }
        public int Size { get; private set; }
        public int StartAddress { get; set; }
        public bool ValueNotInRange { get; set; }
        private bool ConvertingBooleanArray { get; set; }
        private bool IntegerValueChanging { get; set; }
        public bool HasNoUseDataBitsAfter { get; set; }
        public int NumberOfUseDataBits { get; set; }

        public Signal(string name, int size, string initalValue, string[] rawSignalValues = null)
        {
            #region Parameter Validation
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));


            if (string.IsNullOrEmpty(initalValue))
                throw new ArgumentException($"'{nameof(initalValue)}' cannot be null or empty.", nameof(initalValue));

            #endregion

            Name = name;
            Size = size;
            InitalValue = initalValue.ConvertToHex();
            RawSignalValues = rawSignalValues;

            SelectAll = new RelayCommand(SelectAllPressed);
            ClearAll = new RelayCommand(ClearAllPressed);

        }

        public Signal(string name, string size, string initalValue, string[] rawSignalValues = null) : this(name, int.Parse(size), initalValue, rawSignalValues)
        {
        }

        public event EventHandler<int> IntegerValueOutOfRange;

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler UpdateLINMessage;

        public void CreateBitValues(int maxSize, Frame frame)
        {
            if (frame is null)
                throw new ArgumentNullException(nameof(frame));

            Frame         = frame;
            BooleanValues = new ObservableCollection<BoolValues>();

            /// Three things need to achieve
            /// Make a Boolean Values or the max size
            /// Enabled all bits inside boolean values until the Size
            /// Enabled all bits inside boolean values which are not in use but also disable them set no in use property to false
            /// Rest all bits do not enable

            maxSize += NumberOfUseDataBits;

            var bitsToEnable = Size + NumberOfUseDataBits;

            for (int i = 0; i < maxSize; i++)
            {
                BoolValues bit;

                if (i < bitsToEnable)
                {
                    bit = new BoolValues(i, true);

                    if (i >= Size)
                    {
                        bit.InUse = false;
                    }
                }
                else
                {
                    bit = new BoolValues(i, false);
                }
                bit.PerformBooleanToInt += BooleanValueChanged;
                BooleanValues.Add(bit);
            }

            BooleanValues = BooleanValues.OrderByDescending(s => s.Placeholder).ToList();
        }

        public override string ToString() => $"Name: {Name}, Size: {Size}, StartAddress: {StartAddress}, Initial Value: {InitalValue}";

        public void UpdateEncoding(EncodingNode encoding)
        {
            if (encoding is null)
                throw new ArgumentNullException(nameof(encoding));

            Encoding = encoding;
        }

        private void BooleanValueChanged(object sender, EventArgs e)
        {
            if (ConvertingBooleanArray) return;

            IntegerValueChanging = true;

            IntegerValue = 0;

            foreach (var bit in from placeholder in BooleanValues.OrderBy(s => s.Placeholder)
                                where placeholder.Value && placeholder.Enabled
                                select placeholder)
            {
                var power = (int)Math.Pow(2, bit.Placeholder);
                IntegerValue += power;
            }

            UpdateLINMessage?.Invoke(this, null);
            Frame.ConvertToByteArray();
            IntegerValueChanging = false;
        }

        private void ClearAllPressed()
        {
            IntegerValue = 0;
        }

        private void ConvertBooleanArray(int integerValue)
        {
            if (IntegerValueChanging) return;

            ConvertingBooleanArray = true;

            int dividend = integerValue;
            int divisor = 2;
            int counter = 0;

            Dictionary<int, bool> booleans = new Dictionary<int, bool>();

            while (dividend >= 1)
            {
                dividend = Math.DivRem(dividend, divisor, out int result);

                var bitValue = result == 1;
                booleans.Add(counter, bitValue);
                counter++;
            }

            foreach (var bitValue in BooleanValues)
            {
                if (booleans.ContainsKey(bitValue.Placeholder))
                    bitValue.Value = booleans[bitValue.Placeholder];
                else
                    bitValue.Value = false;
            }

            ConvertingBooleanArray = false;
        }

        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        private void SelectAllPressed() => IntegerValue = MaxValue;
    }
}
