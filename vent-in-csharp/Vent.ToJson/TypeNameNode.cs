
namespace Vent.ToJson
{
    public class TypeNameNode
    {
        public static void ResolveForwardReferences(EntityRegistry registry,
            Dictionary<object, List<ForwardReference>> forwardReferences)
        {
            foreach (var forwardReferenceList in forwardReferences)
            {
                foreach (var forwardReference in forwardReferenceList.Value)
                {
                    forwardReference.ResolveEntity(registry, forwardReferenceList.Key);
                }
            }
        }

        public string TypeName { get; set; }

        public List<TypeNameNode> GenericTypeNames { get; set; }

        public TypeNameNode() { }

        public TypeNameNode(string mainTypeName, List<TypeNameNode> genericTypes = null)
        {
            TypeName = mainTypeName;
            GenericTypeNames = genericTypes;
        }

        public Type ResolveType(Dictionary<string, Type> classLookup)
        {
            if (!classLookup.TryGetValue(TypeName, out Type mainType))
            {
                mainType = Type.GetType(TypeName);
            }

            if (GenericTypeNames == null)
            {
                return mainType;
            }

            var genericTypes = GenericTypeNames.Select(subType => subType.ResolveType(classLookup)).ToArray();

            return mainType.MakeGenericType(genericTypes);
        }

        public object CreateInstance(Dictionary<string, Type> classLookup)
        {
            return Activator.CreateInstance(ResolveType(classLookup));
        }       
    }
}
