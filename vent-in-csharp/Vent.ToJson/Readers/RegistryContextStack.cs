/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using Vent.Registry;

namespace Vent.ToJson
{
    public class RegistryContextStack
    {
        public List<RegistryContext> Stack { get; set; } = new List<RegistryContext>();

        public RegistryContext Top { get => Stack.FirstOrDefault(); }

        public RegistryContextStack()
        {
        }

        public RegistryContextStack(RegistryContext intialContext)
        {
            Stack.Add(intialContext);
        }

        public void Push(RegistryContext context)
        {
            Stack.Insert(0, context);
        }

        public void Push(EntityRegistry registry)
        {
            Stack.Insert(0, new RegistryContext(registry));
        }

        public void Pop()
        {
            Stack.RemoveAt(0);
        }
    }
}
