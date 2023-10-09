/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

namespace Vent.ToJson
{
    public class EntitySerializationException : Exception
    {
        public EntitySerializationException() { }

        public EntitySerializationException(string message) : base(message) { }
    }
}
