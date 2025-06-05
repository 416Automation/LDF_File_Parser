using LDF_File_Parser.Extension;
using LDF_File_Parser.Logger;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace LDF_FILEPARSER
{
    public class Frame : INotifyPropertyChanged
    {
        public string ID { get; set; }
        public string LinDisplayMessage { get; set; } = $"ID     : 0x00" + Environment.NewLine + "Data : 0x00";

        public byte[] LinMessage { get; set; }

        public string Name { get; set; }

        public int ResponseLength { get; set; }

        public ObservableCollection<Signal> Signals { get; set; } = new ObservableCollection<Signal>();

        public Frame(string name, string iD, int responseLength)
        {
            #region Parameter Validation
            if (string.IsNullOrEmpty(name))
                throw new System.ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));

            if (string.IsNullOrEmpty(iD))
                throw new System.ArgumentException($"'{nameof(iD)}' cannot be null or empty.", nameof(iD));
            #endregion

            Name = name;
            ID = iD.ToUpper();
            if (ID.StartsWith("0X")) ID = ID.Replace('X', 'x');
            if (ID.StartsWith("0x") == false) ID = ID.ConvertToHex();
            ResponseLength = responseLength;

            LinDisplayMessage = $"ID     : {ID}" + Environment.NewLine + "Data : 0x00";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void ConvertToByteArray()
        {
            try
            {
                List<bool> booleanValuesFromSignals = new List<bool>();

                foreach (var signal in Signals.OrderBy(s => s.StartAddress))
                {
                    booleanValuesFromSignals.AddRange(signal.BooleanValues.OrderBy(k => k.Placeholder).Where(s => s.Enabled == true).Select(s => s.Value).ToList());
                }

                bool[] bools = booleanValuesFromSignals.ToArray();

                byte[] arr1 = Array.ConvertAll(bools, b => b ? (byte)1 : (byte)0);

                // pack (in this case, using the first bool as the lsb - if you want
                // the first bool as the msb, reverse things ;-p)
                int bytes = bools.Length / 8;
                if ((bools.Length % 8) != 0) bytes++;

                LinMessage = new byte[bytes];
                int bitIndex = 0, byteIndex = 0;
                for (int i = 0; i < bools.Length; i++)
                {
                    if (bools[i])
                    {
                        LinMessage[byteIndex] |= (byte)(((byte)1) << bitIndex);
                    }
                    bitIndex++;
                    if (bitIndex == 8)
                    {
                        bitIndex = 0;
                        byteIndex++;
                    }
                }

                StringBuilder bytearrayString = new StringBuilder();

                bytearrayString.AppendLine($"ID     : {ID}");

                var byteString = string.Empty;
                foreach (var byteValue in LinMessage)
                {
                    byteString += string.Format("0x{0:X} ", byteValue.ToString("X2"));
                }

                bytearrayString.AppendLine($"Data : {byteString}");

                LinDisplayMessage = bytearrayString.ToString();
            }
            catch (Exception exc)
            {
                Logger.LogError(exc);
            }
        }

        public override string ToString() => $"{Name} - ID: {ID}";

        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}