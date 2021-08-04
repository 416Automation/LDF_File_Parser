using LDF_File_Parser.Extension;
using System.Collections.Generic;

namespace LDF_FILEPARSER
{
    public class PhyscialEncodingValue : IEncodingValue
    {
        public string Description { get; set; }
        public string HexAddress { get; }
        public ICollection<Signal> Signals { get; }
        public EncodingType Type { get; } = EncodingType.Physical;
        public ICollection<string> Values { get; set; } = new List<string>();
        public PhyscialEncodingValue(string hexAddress)
        {
            if (string.IsNullOrEmpty(hexAddress)) throw new System.ArgumentException($"'{nameof(hexAddress)}' cannot be null or empty.", nameof(hexAddress));

            HexAddress = hexAddress;
        }

        public override string ToString() => $"Type: {Type}, HexAddress: {HexAddress}, Values: {Values.Join()}";
    }
}