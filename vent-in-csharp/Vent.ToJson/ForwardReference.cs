/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Collections;
using System.Reflection;

using Vent.Registry;

namespace Vent.ToJson
{
    /// <summary>
    /// Reference to an entity which is may not be present in the owning Registry yet.
    /// Entities may have properties referring to other entities. The deserialization 
    /// used in Vent.ToJson employs forward deserialization which means entity reference
    /// may point to entities which have not been deserialized yet. If this happens
    /// during deserialization this ForwardEntityReference will be created as a placeholder.
    /// After deserialization completes all references will be resolved. 
    /// </summary>
    public class ForwardEntityReference : EntityBase
    {
        public EntityRegistry Registry { get; set; }

        public int EntityId { get; set; }

        public object Key { get; set; }

        public ForwardEntityReference()
        {
        }

        public ForwardEntityReference(EntityRegistry registry, int entityKey)
        {
            Registry = registry;
            EntityId  = entityKey;
        }

        public ForwardEntityReference(EntityRegistry registry, int entityKey, object key)
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
