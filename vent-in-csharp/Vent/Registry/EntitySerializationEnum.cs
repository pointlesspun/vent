/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Reflection;

namespace Vent.Registry
{
    public enum EntitySerialization
    {
        AsReference,
        AsValue
    };

    public static class EntitySerializationExtensions
    {
        public static EntitySerialization GetEntitySerialization(this PropertyInfo propertyInfo) =>
                Attribute.IsDefined(propertyInfo, typeof(SerializeAsValueAttribute))
                            ? EntitySerialization.AsValue
                            : EntitySerialization.AsReference;
    }
}
