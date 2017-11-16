using System;

namespace SharedData
{
    public class SharedDataException : Exception
    {
        public SharedDataException()
        {
        }

        public SharedDataException(string message)
            : base(message)
        {
        }

        public SharedDataException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
