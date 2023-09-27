using Vent.PropertyEntities;

namespace Vent.ToJson.Test.TestEntities
{
    public class ObjectWrapper<T> : ObjectEntity<T> where T : class
    {
        [SerializeAsValue]
        public override T Value { get => base.Value; set => base.Value = value; }

        public ObjectWrapper() : base() { }

        public ObjectWrapper(T value) : base(value) { }
    }
}
