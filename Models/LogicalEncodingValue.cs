using LDF_File_Parser.Extension;
using System.Collections.Generic;

namespace LDF_FILEPARSER
{
    public class LogicalEncodingValue : IEncodingValue
    {
        public string Description { get; set; }

        public string HexAddress { get; }

        public EncodingType Type { get; } = EncodingType.Logical;

        public ICollection<Signal> Signals { get; }

        public LogicalEncodingValue(string hexAddress, string description)
        {
            #region Parameter Validation

            if (string.IsNullOrEmpty(description))
            {
                throw new System.ArgumentException($"'{nameof(description)}' cannot be null or empty.", nameof(description));
            }

            if (string.IsNullOrEmpty(hexAddress))
            {
                throw new System.ArgumentException($"'{nameof(hexAddress)}' cannot be null or empty.", nameof(hexAddress));
            }
            #endregion
            
            Description = description;
            HexAddress = hexAddress.ConvertToHex();
        }
        public override string ToString() => $"Type: {Type}, HexAddress: {HexAddress}, Description: {Description}";


    }
}