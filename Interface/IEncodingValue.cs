using System.Collections.Generic;

namespace LDF_FILEPARSER
{
    public interface IEncodingValue 
    {
        EncodingType Type { get; }
        string HexAddress { get; }
        ICollection<Signal> Signals { get; }
}
}