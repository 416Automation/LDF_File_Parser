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
        private bool _valueNotInRange = false;


        public ICollection<BoolValues> BooleanValues { get; set; }
        public RelayCommand ClearAll { get; private set; }
        public EncodingNode Encoding { get; set; }
        public Frame Frame { get; private set; }
        public string HexValue
        {
            get => _hexValue; set
            {
                if (_hexValue != value)
                {
                    _hexValue = value;
                    IntegerValue = Convert.ToInt32(_hexValue, 16);
                    ConvertBooleanArray(IntegerValue);
                    NotifyPropertyChanged();
                }
            }
        }

        public string InitalValue { get; private set; }
        public int IntegerValue
        {
            get => _integerValue; set
            {
                if (_integerValue != value)
                {
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
                    NotifyPropertyChanged();
                }
            }
        }

        public string MaxHexValue => MaxValue.ToString().ConvertToHex();
        public int MaxValue => BooleanValues.Where(s => s.Enabled).Sum(k => (int)Math.Pow(2, k.Placeholder));
        public string Name { get; private set; }
        public string[] RawSignalValues { get; }
        public RelayCommand SelectAll { get; set; }
        public int Size { get; private set; }
        public int StartAddress { get; set; }
        public bool ValueNotInRange
        {
            get => _valueNotInRange; private set
            {
                if (_valueNotInRange != value)
                {
                    _valueNotInRange = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private bool ConvertingBooleanArray { get; set; } = false;

        private bool IntegerValueChanging { get; set; } = false;

        public Signal(string name, string size, string initalValue, string[] rawSignalValues)
        {

            #region Parameter Validation
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));

            if (string.IsNullOrEmpty(size))
                throw new ArgumentException($"'{nameof(size)}' cannot be null or empty.", nameof(size));

            if (string.IsNullOrEmpty(initalValue))
                throw new ArgumentException($"'{nameof(initalValue)}' cannot be null or empty.", nameof(initalValue));

            if (rawSignalValues is null)
                throw new ArgumentNullException(nameof(rawSignalValues));
            #endregion

            Name = name;
            Size = int.Parse(size);
            InitalValue = initalValue.ConvertToHex();
            RawSignalValues = rawSignalValues;

            SelectAll = new RelayCommand(SelectAllPressed);
            ClearAll = new RelayCommand(ClearAllPressed);

        }

        public event EventHandler<int> IntegerValueOutOfRange;

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler UpdateLINMessage;

        public void CreateBitValues(int maxSize, Frame frame)
        {
            if (frame is null)
                throw new ArgumentNullException(nameof(frame));

            Frame = frame;

            BooleanValues = new ObservableCollection<BoolValues>();

            for (int i = maxSize; i >= 0; i--)
            {
                int placeholder = (int)Math.Pow(2, i);

                if (i < Size)
                {
                    BoolValues bit = new BoolValues(i, true);
                    bit.PerformBooleanToInt += BooleanValueChanged;
                    BooleanValues.Add(bit);
                }
                else
                {
                    BoolValues bit = new BoolValues(i);
                    bit.PerformBooleanToInt += BooleanValueChanged;
                    BooleanValues.Add(bit);
                }
            }
            BooleanValues.OrderBy(s => s.Placeholder);
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

        private void ClearAllPressed() => IntegerValue = 0;

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

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SelectAllPressed() => IntegerValue = MaxValue;
    }
}
