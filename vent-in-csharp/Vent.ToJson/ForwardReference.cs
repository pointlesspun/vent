using System.Collections;
using System.Reflection;

namespace Vent.ToJson
{
    public class ForwardReference
    {
        public EntityRegistry Registry { get; set; }

        public int EntityId { get; set; }

        public object Key { get; set; }

        public ForwardReference()
        {
        }

        public ForwardReference(EntityRegistry registry, int entityKey)
        {
            Registry = registry;
            EntityId  = entityKey;
        }

        public ForwardReference(EntityRegistry registry, int entityKey, object key)
        {
            Registry = registry;
            EntityId = entityKey;
            Key = key;
        }

        public void ResolveEntity(object target)
        {
            if (target is IList list)
            {
                list[(int)Key] = Registry[EntityId];
            }
            else if (target is IDictionary dictionary)
            {
                dictionary[Key] = Registry[EntityId];
            }
            else if (Key is PropertyInfo propertyInfo)
            {
                propertyInfo.SetValue(target, Registry[EntityId]);
            }
        }
    }
}
