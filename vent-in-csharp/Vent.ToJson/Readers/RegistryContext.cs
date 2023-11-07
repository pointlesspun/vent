/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using Vent.Registry;
using Vent.ToJson.Readers;

namespace Vent.ToJson
{
    /// <summary>
    /// Context for the current registry being used while creating objects from a json file
    /// </summary>
    public class RegistryContext
    {
        /// <summary>
        /// Registry of entities
        /// </summary>
        public EntityRegistry Registry { get; set; }

        /// <summary>
        /// Forward references found during this pass
        /// </summary>
        public Dictionary<object, List<ForwardEntityReference>> ForwardReferenceLookup { get; set; }

        public RegistryContext()
        {
        }

        public RegistryContext(EntityRegistry registry)
        {
            Registry = registry;
        }

        /// <summary>
        /// Add a forward reference
        /// </summary>
        /// <param name="target">Object to apply the forward reference to (eg entity, object, list or dictionary) </param>
        /// <param name="reference">Forward reference to an entity which is not yet in the registry </param>
        public void AddReference(object target, ForwardEntityReference reference)
        {
            ForwardReferenceLookup ??= new Dictionary<object, List<ForwardEntityReference>>();

            if (!ForwardReferenceLookup.TryGetValue(target, out List<ForwardEntityReference> references)) 
            {
                references = new List<ForwardEntityReference>();
                ForwardReferenceLookup.Add(target, references);
            }

            references.Add(reference);
        }
    }
}
