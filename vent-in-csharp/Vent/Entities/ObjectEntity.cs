/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun
namespace Vent.Entities
{
    public class ObjectEntity<T> : PropertyEntity<T> where T : class
    {
        public ObjectEntity() { }

        public ObjectEntity(T value) : base(value) { }

        public override bool Equals(object obj)
        {
            return obj is ObjectEntity<T> entity 
                   && Id == entity.Id 
                   && (Value == entity.Value || (Value != null && Value.Equals(entity.Value)));
        }

        public override object Clone()
        {
            var clone = (ObjectEntity<T>) base.Clone();

            if (Value != null && Value is ICloneable cloneable)
            {
                clone.Value = (T) cloneable.Clone();
            }

            return clone;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Value);
        }

    }
}
