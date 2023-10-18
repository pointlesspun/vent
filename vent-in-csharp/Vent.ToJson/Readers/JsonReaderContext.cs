/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using Vent.Registry;
using Vent.ToJson.Readers;

namespace Vent.ToJson
{
    public class JsonReaderContext
    {
        public Dictionary<string, Type> ClassLookup { get; set; }

        public RegistryContextStack RegistryStack { get; set; } = new RegistryContextStack();

        public EntityRegistry TopRegistry => RegistryStack.Top.Registry;

        public RegistryContext Top => RegistryStack.Top;

        public Dictionary<object, List<ForwardEntityReference>> TopLookup => RegistryStack.Top.ForwardReferenceLookup;

        public JsonReaderContext() 
        {
            RegistryStack = new RegistryContextStack();
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
            RegistryStack = new RegistryContextStack(registry);
            ClassLookup = classLookup;
        }

        public void Push(RegistryContext context)
        {
            RegistryStack.Push(context);
        }

        public void Push(EntityRegistry registry)
        {
            RegistryStack.Push(new RegistryContext(registry));
        }

        public void Pop()
        {
            RegistryStack.Pop();
        }
    }
}
