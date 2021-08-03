using System;
using System.Runtime.Serialization;

namespace LDF_File_Parser.Exceptions
{
    /// <summary>
    /// Encoding type not found in defined encoding types
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class InvalidEncodingTypeException : Exception
    {
        public InvalidEncodingTypeException()
        {
        }

        public InvalidEncodingTypeException(string message) : base(message)
        {
        }

        public InvalidEncodingTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidEncodingTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
