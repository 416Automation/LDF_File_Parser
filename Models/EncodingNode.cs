using LDF_File_Parser.Extension;
using System.Collections.Generic;

namespace LDF_FILEPARSER
{
    public class EncodingNode
    {
        public ICollection<IEncodingValue> EncodingTypes { get; } = new List<IEncodingValue>();

        public string Name { get;  set; } = "N/A";

        public EncodingNode(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new System.ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));

            Name = name;
        }
        public override string ToString() => $"Name: {Name}, EncodingTypes: {EncodingTypes.Join()}";

    }
}