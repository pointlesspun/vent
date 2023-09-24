using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vent.PropertyEntities
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
