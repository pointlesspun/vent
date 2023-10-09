/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

namespace Vent.Entities
{
    public class StringEntity : PropertyEntity<string>
    {
        public StringEntity() { }

        public StringEntity(string value) : base(value) { }

        public override bool Equals(object obj)
        {
            return obj is StringEntity entity &&
                   Id == entity.Id &&
                   Value == entity.Value;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
