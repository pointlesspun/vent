/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Collections;
using System.Reflection;

using Vent.Registry;
using Vent.Util;

namespace Vent.ToJson.Readers
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
        /// <summary>
        /// Registry in which to find the reference
        /// </summary>
        public EntityRegistry Registry { get; set; }

        /// <summary>
        /// Id of the entity which needs to be resolved
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// Key used to set the entity reference to the entity. This key depends
        /// on the entity 'container', aka the target. When the target is a list
        /// or dictionary the key is an index, if the target is an object, the key
        /// is a PropertyInfo.
        /// </summary>
        public object Key { get; set; }

        public ForwardEntityReference()
        {
        }

        public ForwardEntityReference(EntityRegistry registry, int entityKey)
        {
            Registry = registry;
            EntityId = entityKey;
        }

        public ForwardEntityReference(EntityRegistry registry, int entityKey, object key)
        {
            Registry = registry;
            EntityId = entityKey;
            Key = key;
        }

        /// <summary>
        /// Given a target (an entity 'container'), set the target's entity based on the 
        /// properties of this ForwardReference
        /// </summary>
        /// <param name="target"></param>
        public void ResolveEntity(object target)
        {
            Contract.NotNull(target);

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
