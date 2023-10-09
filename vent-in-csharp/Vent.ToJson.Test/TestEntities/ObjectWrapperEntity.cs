/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using Vent.Entities;
using Vent.Registry;

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
