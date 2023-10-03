/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Collections;
using System.Reflection;

namespace Vent
{
    public static class EntityReflection
    {
        public static void CopyPropertiesFrom(this IEntity to, IEntity from)
        {
            Contract.NotNull(to);
            Contract.NotNull(from);
            Contract.Requires(to != from);

            Type typeA = from.GetType();
            Type typeB = to.GetType();

            PropertyInfo[] propertiesA = typeA.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo[] propertiesB = typeB.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo propA in propertiesA)
            {
                PropertyInfo propB = Array.Find(propertiesB,
                    p => p.Name == propA.Name && p.PropertyType == propA.PropertyType);

                if (propB != null && propB.CanWrite)
                {
                    var valueA = propA.GetValue(from);
                    
                    if (valueA != null)
                    {
                        var valueACopy = CanDeepCopy(propA, valueA) 
                                            ? DeepCopyValue(valueA) 
                                            : valueA;

                        propB.SetValue(to, valueACopy);
                    }
                }
            }
        }

        // https://stackoverflow.com/questions/374651/how-to-check-if-an-object-is-nullable
        public static bool IsNullable<T>(T obj) =>
            obj == null || IsNullableType(typeof(T));
        

        // https://stackoverflow.com/questions/374651/how-to-check-if-an-object-is-nullable
        public static bool IsNullableType(this Type type) =>
            (!type.IsValueType) || (Nullable.GetUnderlyingType(type) != null);
        
        private static bool CanDeepCopy(PropertyInfo info, object value)
            => value != null && !IsEntityOrPrimitiveOrString(info.PropertyType);

        private static bool CanDeepCopy(Type t, object value)
            => value != null && !IsEntityOrPrimitiveOrString(t);

        public static bool IsEntityOrPrimitiveOrString(Type propertyType) =>
                   IsPrimitiveOrString(propertyType)
                || IsEntity(propertyType);

        public static bool IsEntity(Type propertyType) =>
            typeof(IEntity).IsAssignableFrom(propertyType);

        public static bool IsPrimitiveOrString(Type propertyType) =>
            propertyType.IsPrimitive
                || propertyType == typeof(string);

        private static object DeepCopyValue(object source)
        {
            // assert not null && not is primite or string
            Type type = source.GetType();

            if (type.IsArray)
            {
                return DeepCopy((Array)source);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    return DeepCopy((IList)source);
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    return DeepCopy((IDictionary)source);
                }
            }

            // Fallback to MemberwiseClone for custom objects
            MethodInfo cloneMethod = type.GetMethod(nameof(MemberwiseClone), BindingFlags.NonPublic | BindingFlags.Instance);

            if (cloneMethod != null)
            {
                object clonedObject = cloneMethod.Invoke(source, null);
                return clonedObject;
            }

            throw new NotSupportedException($"Deep copy is not supported for type: {type.FullName}");
        }

        public static Array DeepCopy(Array source)
        {
            var type = source.GetType();
            var elementType = type.GetElementType();
            var copiedArray = Array.CreateInstance(elementType, source.Length);

            for (int i = 0; i < source.Length; i++)
            {
                var element = source.GetValue(i);
                var canDeepCopy = CanDeepCopy(elementType, element);
                var elementCopy = canDeepCopy ? DeepCopyValue(element) : element;

                copiedArray.SetValue(elementCopy, i);
            }

            return copiedArray;
        }

        public static IList DeepCopy(IList sourceList)
        {
            var type = sourceList.GetType();
            var elementType = type.GetGenericArguments()[0];
            var listType = typeof(List<>).MakeGenericType(elementType);
            var copiedList = (IList)Activator.CreateInstance(listType);

            foreach (object element in sourceList)
            {
                var canDeepCopy = CanDeepCopy(elementType, element);
                object elementCopy = canDeepCopy ? DeepCopyValue(element) : element;
                copiedList.Add(elementCopy);
            }

            return copiedList;
        }

        public static IDictionary DeepCopy(IDictionary sourceDictionary)
        {
            var type = sourceDictionary.GetType();
            var keyType = type.GetGenericArguments()[0];
            var valueType = type.GetGenericArguments()[1];
            var dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            var copiedDict = (IDictionary)Activator.CreateInstance(dictType);

            foreach (DictionaryEntry entry in sourceDictionary)
            {
                var canDeepCopyKey = entry.Key != null && CanDeepCopy(entry.Key.GetType(), entry.Key);
                var canDeepCopyValue = entry.Value != null && CanDeepCopy(entry.Value.GetType(), entry.Value);

                object keyCopy = canDeepCopyKey ? DeepCopyValue(entry.Key) : entry.Key;
                object valueCopy = canDeepCopyValue ? DeepCopyValue(entry.Value) : entry.Value;
                
                copiedDict.Add(keyCopy, valueCopy);
            }

            return copiedDict;
        }
    }
}
