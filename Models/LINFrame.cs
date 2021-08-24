using LDF_File_Parser.Extension;
using System.Collections.Generic;
using System.Windows.Controls;

namespace LDF_FILEPARSER
{
    public class LINFrame
    {
        public string ID { get; set; }

        public string Name { get; set; }

        public int ResponseLength { get; set; }

        public ICollection<Signal> Signals { get; set; } = new HashSet<Signal>();

        public LINFrame(string name, string iD, int responseLength)
        {
            #region Parameter Validation
            if (string.IsNullOrEmpty(name))
                throw new System.ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));

            if (string.IsNullOrEmpty(iD))
                throw new System.ArgumentException($"'{nameof(iD)}' cannot be null or empty.", nameof(iD));
            #endregion

            Name = name;
            ID = iD.ConvertToHex();
            ResponseLength = responseLength;
        }
        public override string ToString() => $"Name: {Name} - ID: {ID}";
    }
}