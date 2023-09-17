/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Diagnostics;

namespace Vent
{
    public class ContractException : Exception
    {
        public ContractException() : base() { }
        public ContractException(string message) : base(message) { }
    }

    public static class Contract
    {
        /// <summary>
        /// Condition b needs to be true or a ContractException will be thrown
        /// </summary>
        /// <param name="b"></param>
        /// <param name="message"></param>
        /// <exception cref="ContractException"></exception>
        [Conditional("DEBUG")]
        public static void Requires(bool b, string message = null)
        {
            if (!b)
            {
                if (message == null)
                {
                    throw new ContractException();
                }

                throw new ContractException(message);
            }
        }

        public static void Requires<T>(bool b, string message = null) where T : Exception, new()
        {
            if (!b)
            {
                if (message == null) 
                {
                    throw new T();
                }

                throw (Exception) Activator.CreateInstance(typeof(T), message);
            }
        }

        /// <summary>
        /// Object o needs to be not null or a ContractException will be thrown
        /// </summary>
        /// <param name="o"></param>
        /// <param name="message"></param>
        [Conditional("DEBUG")]
        public static void NotNull(object o, string message = null)
        {
            Requires(o != null, message ?? "object is null");
        }
    }
}

