using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vent.ToJson
{
    public static class VentClassName
    {
        public static string ToVentClassName(this Type type)
        {
            var stringBuilder = new StringBuilder();

            if (type.IsGenericType)
            {
                stringBuilder.Append(string.Concat(type.Namespace, ".", type.Name.AsSpan(0, type.Name.Length - 2)));

                var genericArgs = type.GetGenericArguments();

                if (genericArgs != null && genericArgs.Length > 0)
                {
                    var concreteArgs = genericArgs.Where(arg => !arg.IsGenericParameter).Select(ToVentClassName);

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

        public static object CreateInstance(this string className, Dictionary<string, Type> classLookup)
        {
            return ParseVentClassName(className).CreateInstance(classLookup);
        }


        public static TypeNameNode ParseVentClassName(this string argString)
        {
            if (argString == null)
            {
                return null;
            }

            var genericArgsIndex = argString.IndexOf('<');

            if (genericArgsIndex < 0)
            {
                return new TypeNameNode(argString.Trim());
            }

            return new TypeNameNode(argString.Substring(0, genericArgsIndex), ParseGenericArgs(argString.Substring(genericArgsIndex)).node);
        }

        public static (List<TypeNameNode> node, int currentIndex) ParseGenericArgs(this string argString, int index = 0)
        {
            if (argString == null)
            {
                return (null, -1);
            }

            StringBuilder builder = null;
            List<TypeNameNode> result = null;
            TypeNameNode currentNode = null;

            void PushNode()
            {
                result ??= new List<TypeNameNode>();
                currentNode ??= new TypeNameNode(builder.ToString());
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
                            currentNode = new TypeNameNode(builder.ToString());
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
                            currentNode ??= new TypeNameNode(builder.ToString());
                            builder = null;
                            var (genericTypes, idx) = ParseGenericArgs(argString, i);
                            currentNode.GenericTypeNames = genericTypes;
                            i = idx;
                        }
                        break;
                    case '>':
                        if (currentNode == null && builder != null && builder.Length > 0)
                        {
                            currentNode = new TypeNameNode(builder.ToString());
                        }

                        if (currentNode != null)
                        {
                            result ??= new List<TypeNameNode>();
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
