/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

namespace Vent.ToJson
{
    /// <summary>
    /// Exception thrown when a de/serialization runs into an exception
    /// </summary>
    public class EntitySerializationException : Exception
    {
        public EntitySerializationException() { }

        public EntitySerializationException(string message) : base(message) { }
    }
}
