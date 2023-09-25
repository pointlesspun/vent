using System.Reflection;

namespace Vent.ToJson
{
    public static class ClassLookup
    {
        public static Dictionary<string, Type> CreateFrom(params Assembly[] assemblies)
        {
            var entityType = typeof(IEntity);
            var classLookup = new Dictionary<string, Type>();

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes().Where(p =>
                    p.IsClass && !p.IsAbstract && p.IsPublic && !p.IsInterface))
                {
                    classLookup[type.ToVentClassName()] = type;
                }
            }

            return classLookup;
        }


        public static Dictionary<string, Type> WithType(this Dictionary<string, Type> classLookup, Type type)
        {
            classLookup[type.ToVentClassName()] = type;
            return classLookup;
        }
    }
}
