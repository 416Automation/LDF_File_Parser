using System;
using System.Runtime.Serialization;

namespace LDF_File_Parser.Exceptions
{
    /// <summary>
    /// The encoding name is not found from the list of defined encodings extracted from LDF File
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class EncodingNameNotFoundException : Exception
    {
        public EncodingNameNotFoundException()
        {
        }

        public EncodingNameNotFoundException(string message) : base(message)
        {
        }

        public EncodingNameNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected EncodingNameNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
