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
                // we need classes which can be used in construction via reflection
                // only accept public, none static classes. We do accept interfaces because we need 
                // to be able to generate something like PropertyEntity<IEntity>
                foreach (var type in assembly.GetTypes().Where(p => p.IsPublic && !(p.IsAbstract && p.IsSealed)))
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
