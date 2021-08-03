using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LDF_File_Parser.Exceptions
{
    /// <summary>
    /// Signal not found from the defined signals in the LDF File
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class SignalNotFoundException : Exception
    {
        public SignalNotFoundException()
        {
        }

        public SignalNotFoundException(string message) : base(message)
        {
        }

        public SignalNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SignalNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
