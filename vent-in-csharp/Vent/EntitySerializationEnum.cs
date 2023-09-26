using System.Reflection;

namespace Vent
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
