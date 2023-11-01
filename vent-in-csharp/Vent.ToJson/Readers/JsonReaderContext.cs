/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using Vent.Registry;
using Vent.ToJson.Readers;

namespace Vent.ToJson
{
    public class JsonReaderContext
    {
        public Dictionary<string, Type> ClassLookup { get; set; }

        //public RegistryContextStack RegistryStack { get; set; } = new RegistryContextStack();
        public List<RegistryContext> RegistryStack { get; set; } 

        public EntityRegistry TopRegistry => RegistryStack[0].Registry;

        public RegistryContext Top => RegistryStack[0];

        public Dictionary<object, List<ForwardEntityReference>> TopLookup => RegistryStack[0].ForwardReferenceLookup;

        public JsonReaderContext() 
        {
            RegistryStack = new List<RegistryContext>();
        }

        public JsonReaderContext(Dictionary<string, Type> classLookup)
        {
            ClassLookup = classLookup;
        }

        public JsonReaderContext(EntityRegistry registry, Dictionary<string, Type> classLookup)
            : this(new RegistryContext(registry), classLookup)
        {
        }

        public JsonReaderContext(RegistryContext registry, Dictionary<string, Type> classLookup) 
        {
            RegistryStack = new List<RegistryContext>() { registry };
            ClassLookup = classLookup;
        }

        public void Push(RegistryContext context)
        {
            RegistryStack.Insert(0, context);
        }

        public void Push(EntityRegistry registry)
        {
            RegistryStack.Insert(0, new RegistryContext(registry));
        }

        public void Pop()
        {
            RegistryStack.RemoveAt(0);
        }
    }
}
