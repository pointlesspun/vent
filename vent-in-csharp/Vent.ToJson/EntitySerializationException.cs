namespace Vent.ToJson
{
    public class EntitySerializationException : Exception
    {
        public EntitySerializationException() { }

        public EntitySerializationException(string message) : base(message) { }
    }
}
