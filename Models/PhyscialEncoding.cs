using System.Collections.Generic;

namespace LDF_FILEPARSER
{
    public class PhyscialEncoding : IEncoding
    {
        public string HexAddress { get; }
        public string Name { get; set; }
        public EncodingType Type { get; } = EncodingType.Physical;
        public IReadOnlyCollection<string> Values { get; set; } = new List<string>();
    }
}