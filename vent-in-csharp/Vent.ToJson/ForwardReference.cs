using System.Collections;
using System.Reflection;

namespace Vent.ToJson
{
    public class ForwardReference
    {
        public int EntityId { get; set; }

        public object Key { get; set; }

        public ForwardReference()
        {
        }

        public ForwardReference(int entityKey)
        {
            EntityId  = entityKey;
        }

        public ForwardReference(int entityKey, object key)
        {
            EntityId = entityKey;
            Key = key;
        }

        public void ResolveEntity(EntityRegistry registry, object target)
        {
            if (target is IList list)
            {
                list[(int)Key] = registry[EntityId];
            }
            else if (target is IDictionary dictionary)
            {
                dictionary[Key] = registry[EntityId];
            }
            else if (Key is PropertyInfo propertyInfo)
            {
                propertyInfo.SetValue(target, registry[EntityId]);
            }
        }
    }
}
