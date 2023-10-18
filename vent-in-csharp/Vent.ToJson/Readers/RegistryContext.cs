/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using Vent.Registry;
using Vent.ToJson.Readers;

namespace Vent.ToJson
{
    public class RegistryContext
    {
        public EntityRegistry Registry { get; set; }

        public Dictionary<object, List<ForwardEntityReference>> ForwardReferenceLookup { get; set; }

        public RegistryContext()
        {
        }

        public RegistryContext(EntityRegistry registry)
        {
            Registry = registry;
        }

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

        public bool ContainsTarget(object target)
        {
            return ForwardReferenceLookup.ContainsKey(target);
        }
    }
}
