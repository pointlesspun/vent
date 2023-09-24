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

        // xxx to do instantiate type
        public object CreateInstance()
        {
            return null;
        }
    }

    public class ParseUtil
    {

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
                            //PushNode();
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
