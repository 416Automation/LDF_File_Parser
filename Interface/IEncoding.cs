namespace LDF_FILEPARSER
{
    public interface IEncoding 
    {

        string Name { get; }
        EncodingType Type { get; }
        string HexAddress { get; }
    }
}