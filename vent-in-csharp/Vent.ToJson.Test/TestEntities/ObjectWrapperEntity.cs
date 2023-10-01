using Vent.PropertyEntities;

namespace Vent.ToJson.Test.TestEntities
{
    public class ObjectWrapperEntity<T> : ObjectEntity<T> where T : class
    {
        [SerializeAsValue]
        public override T Value { get => base.Value; set => base.Value = value; }

        public ObjectWrapperEntity() : base() { }

        public ObjectWrapperEntity(T value) : base(value) { }
    }
}
