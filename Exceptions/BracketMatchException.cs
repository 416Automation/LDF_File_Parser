using System;
using System.Runtime.Serialization;

namespace LDF_File_Parser.Exceptions
{
    /// <summary>
    /// The matched bracket count is not the same as the node matched count
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class BracketMatchException : Exception
    {
        public BracketMatchException()
        {
        }

        public BracketMatchException(string message) : base(message)
        {
        }

        public BracketMatchException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BracketMatchException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
