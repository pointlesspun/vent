/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using Vent.ToJson.Readers;

namespace Vent.ToJson.ClassResolver
{
    /// <summary>
    /// In order to make type names more readable (and short) when exposed to json, specifically generic types, 
    /// we need a simplified form. This class holds the simplified type name and generic types in a tree like structure
    /// (at the cost of keeping track of the assembly version).
    /// This structure in turns can be used to create instances of the type via a ClassLookup.
    /// 
    /// eg given a class name "Foo" in assembly "Comp.Assembly" the type node will be
    /// * TypeName: "Comp.Assembly.Foo"
    /// 
    /// given a class name "Bar<Foo, Qaz<Thud>>" in assembly "Comp.Assembly" the type node will be
    /// * TypeName: "Comp.Assembly.Bar"
    ///     * GenericTypeNames
    ///         * TypeName "Comp.Assembly.Foo"
    ///         * TypeName "Comp.Assembly.Qaz"
    ///             * GenericTypeNames
    ///                 * TypeName "Comp.Assembly.Thud"
    /// </summary>
    public class TypeNameNode
    {
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
