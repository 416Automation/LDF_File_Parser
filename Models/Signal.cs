using LDF_File_Parser.Extension;
using System;
using System.Collections.Generic;

namespace LDF_FILEPARSER
{

    public class Signal
    {
        public int Address { get; set; }
        public IReadOnlyCollection<bool> BitValues { get; set; }
        public string InitalValue { get; private set; }
        public string Name { get; private set; }
        public string[] RawSignalValues { get; }
        public int Size { get; private set; }
        public int StartAddress { get; set; }

        public Signal(string name, string size, string initalValue, string[] rawSignalValues)
        {

            #region Parameter Validation
            if (string.IsNullOrEmpty(name))
                throw new System.ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));

            if (string.IsNullOrEmpty(size))
                throw new System.ArgumentException($"'{nameof(size)}' cannot be null or empty.", nameof(size));

            if (string.IsNullOrEmpty(initalValue))
                throw new System.ArgumentException($"'{nameof(initalValue)}' cannot be null or empty.", nameof(initalValue));

            if (rawSignalValues is null)
                throw new ArgumentNullException(nameof(rawSignalValues));


            #endregion

            Name = name;
            Size = int.Parse(size);
            InitalValue = initalValue.ConvertToHex();
            RawSignalValues = rawSignalValues;
            BitValues = new List<bool>(Size);
        }

        public override string ToString() => $"Name: {Name}, Size: {Size}, StartAddress: {StartAddress}, Initial Value: {InitalValue}";
    }
}