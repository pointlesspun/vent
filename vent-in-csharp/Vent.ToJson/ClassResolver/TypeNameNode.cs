using Vent.ToJson.Readers;
/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun
namespace Vent.ToJson.ClassResolver
{
    public class TypeNameNode
    {
        public static void ResolveForwardReferences(
            Dictionary<object, List<ForwardEntityReference>> forwardReferences)
        {
            foreach (var forwardReferenceList in forwardReferences)
            {
                foreach (var forwardReference in forwardReferenceList.Value)
                {
                    forwardReference.ResolveEntity(forwardReferenceList.Key);
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
            var type = ResolveType(classLookup);
            if (type != null)
            {
                return Activator.CreateInstance(type);
            }
            // xxx to do add full name
            throw new NotImplementedException($"Cannot find type {TypeName}");
        }
    }
}
