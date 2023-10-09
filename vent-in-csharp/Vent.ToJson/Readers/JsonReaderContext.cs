﻿/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using Vent.Registry;

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
