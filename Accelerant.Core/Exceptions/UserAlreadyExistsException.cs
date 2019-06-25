using System;

namespace Accelerant.Core.Exceptions
{
    [Serializable]
    public class UserAlreadyExistsException : Exception
    {
        public UserAlreadyExistsException(string message) : base(message) { }
        public UserAlreadyExistsException(string message, Exception inner) : base(message, inner) { }
    }
}
