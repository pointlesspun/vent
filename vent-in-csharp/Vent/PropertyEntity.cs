/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

namespace Vent
{
    /// <summary>
    /// Entity containing a single property. While not required, it's recommend
    /// to limit the value to a primitive or string as this entity implements
    /// Clone using a memberwise clone.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PropertyEntity<T> : EntityBase
    {
        public virtual T Value { get; set; }

        public PropertyEntity() 
        {
        }

        public PropertyEntity(T value)
        {
            Value = value;
        }

        public PropertyEntity<T> With(T value)
        {
            Value = value;
            return this;
        }

        public override string ToString()
        {
            return $"property: {Value}"; 
        }
    }
}
