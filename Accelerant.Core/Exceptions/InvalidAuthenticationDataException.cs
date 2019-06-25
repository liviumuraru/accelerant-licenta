using System;

namespace Accelerant.Core.Exceptions
{

    [Serializable]
    public class InvalidAuthenticationDataException : Exception
    {
        public InvalidAuthenticationDataException(string message) : base(message) { }
        public InvalidAuthenticationDataException(string message, Exception inner) : base(message, inner) { }
    }
}
