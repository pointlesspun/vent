/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using Vent.Registry;
using Vent.ToJson.Readers;

namespace Vent.ToJson
{
    /// <summary>
    /// While reading json files we need to keep track of the active registry
    /// and the class lookup. This context contains both. 
    /// </summary>
    public class JsonReaderContext
    {
        /// <summary>
        /// List of allowed classes which can be instatiated from a json file.
        /// Types im a json which are not in the class context will throw 
        /// an exception while deserializing.
        /// </summary>
        public Dictionary<string, Type> ClassLookup { get; set; }

        /// <summary>
        /// Stack of context, each time a new registry is encountered in the json file a
        /// new context will be added
        /// </summary>
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

        /// <summary>
        /// Push a new registry onto the top of the stack
        /// </summary>
        /// <param name="registry"></param>
        public void Push(EntityRegistry registry)
        {
            RegistryStack.Insert(0, new RegistryContext(registry));
        }

        /// <summary>
        /// Remove the top registry from the stack
        /// </summary>
        public void Pop()
        {
            RegistryStack.RemoveAt(0);
        }
    }
}
