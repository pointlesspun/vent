using System.Reflection;
using System.Text;

namespace Vent.ToJson
{
    public class TypeNode
    {
        public string MainTypeName { get; set; }

        public List<TypeNode> GenericTypes { get; set; }

        public TypeNode() { }

        public TypeNode(string mainTypeName, List<TypeNode> genericTypes = null)
        {
            MainTypeName = mainTypeName;
            GenericTypes = genericTypes;
        }

        public Type ResolveType(Dictionary<string, Type> classLookup)
        {
            Type mainType = null;

            if (!classLookup.TryGetValue(MainTypeName, out mainType))
            {
                mainType = Type.GetType(MainTypeName);
            }

            if (GenericTypes == null)
            {
                return mainType;
            }

            var genericTypes = GenericTypes.Select(subType => subType.ResolveType(classLookup)).ToArray();

            return mainType.MakeGenericType(genericTypes);
        }

        public object CreateInstance(Dictionary<string, Type> classLookup)
        {
            return Activator.CreateInstance(ResolveType(classLookup));
        }
    }

    public static class ParseUtil
    {
        public static Dictionary<string, Type> WithType(this Dictionary<string, Type> classLookup, Type type)
        {
            classLookup[GetVentClassName(type)] = type;
            return classLookup;
        }

        public static Dictionary<string, Type> CreateClassLookup(params Assembly[] assemblies)
        {
            var entityType = typeof(IEntity);
            var classLookup = new Dictionary<string, Type>();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes().Where(p =>
                    p.IsClass && !p.IsAbstract && p.IsPublic && !p.IsInterface))
                    //&& entityType.IsAssignableFrom(p)))
                {
                    classLookup[GetVentClassName(type)] = type;
                }
            }

            return classLookup;
        }
        

        public static string GetVentClassName(this Type type)
        {
            var stringBuilder = new StringBuilder();

            if (type.IsGenericType)
            {
                stringBuilder.Append(type.Namespace + "." + type.Name.Substring(0, type.Name.Length - 2));
                var genericArgs = type.GetGenericArguments();
                if (genericArgs != null && genericArgs.Length > 0)
                {
                    var concreteArgs = genericArgs.Where(arg => !arg.IsGenericParameter).Select(GetVentClassName);

                    if (concreteArgs.Any())
                    {
                        stringBuilder.Append('<');
                        stringBuilder.Append(string.Join(",", concreteArgs));
                        stringBuilder.Append('>');
                    }
                }
            }
            else
            {
                stringBuilder.Append(type.Namespace + "." + type.Name);
            }

            return stringBuilder.ToString();
        }

        public static TypeNode ParseVentClassName(string argString)
        {
            if (argString == null)
            {
                return null;
            }

            var genericArgsIndex = argString.IndexOf('<');

            if (genericArgsIndex < 0) 
            {
                return new TypeNode(argString.Trim());
            }

            return new TypeNode(argString.Substring(0, genericArgsIndex), ParseGenericArgs(argString.Substring(genericArgsIndex)).node);
        }


        public static (List<TypeNode> node, int currentIndex) ParseGenericArgs(string argString, int index = 0)
        {
            if (argString == null)
            {
                return (null, -1);
            }

            StringBuilder builder = null;
            List<TypeNode> result = null;
            TypeNode currentNode = null;

            void PushNode()
            {
                result ??= new List<TypeNode>();
                currentNode ??= new TypeNode(builder.ToString());
                result.Add(currentNode);
            }

            for (var i = index; i < argString.Length; i++)
            {
                var c = argString[i];
                switch (c)
                {
                    case ' ':
                        if (builder != null && builder.Length > 0 && currentNode == null)
                        {
                            currentNode = new TypeNode(builder.ToString());
                            builder = null;
                        }
                        break;
                    case ',':
                        PushNode();
                        currentNode = null;
                        builder = new StringBuilder();
                        break;
                    case '<':
                        if (builder == null && currentNode == null)
                        {
                            builder = new StringBuilder();
                        }
                        else
                        {                          
                            currentNode ??= new TypeNode(builder.ToString());
                            builder = null;
                            var (genericTypes, idx) = ParseGenericArgs(argString, i);
                            currentNode.GenericTypes = genericTypes;
                            i = idx;
                        }
                        break;
                    case '>':
                        if (currentNode == null && builder != null && builder.Length > 0)
                        {
                            currentNode = new TypeNode(builder.ToString());
                        }

                        if (currentNode != null)
                        {
                            result ??= new List<TypeNode>();
                            result.Add(currentNode);
                        }

                        return (result, i);
                    default:
                        if (builder == null)
                        {
                            throw new Exception(" xxx ");
                        }
                        builder.Append(c);
                        break;
                }
            }

            if (builder != null)
            {
                throw new Exception("bla");
            }

            return (null, -1);
        }      
    }
}
