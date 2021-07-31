using System.Collections.Generic;

namespace LDF_FILEPARSER
{

    public class EncodingNode 
    {
        public string Name { get; }
        public ICollection<IEncoding> EncodingTypes { get; } = new List<IEncoding>();
    }



    public class LogicalEncoding : IEncoding
    {
        public string Description { get; set; }
        public string HexAddress { get; }
        public string Name { get; set; }
        public EncodingType Type { get; } = EncodingType.Logical;
    }
}