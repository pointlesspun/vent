﻿using System.Text;

namespace Vent.ToJson
{
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
            return Activator.CreateInstance(ResolveType(classLookup));
        }       
    }
}
