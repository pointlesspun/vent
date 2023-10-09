/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Reflection;
using Vent.Registry;

namespace Vent.ToJson
{
    public static class ClassLookup
    {
        public static Dictionary<string, Type> CreateFrom(params Assembly[] assemblies)
        {
            return CreateFrom((IEnumerable<Assembly>) assemblies);
        }

        public static Dictionary<string, Type> CreateFrom(IEnumerable<Assembly> assemblies)
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

        public static Dictionary<string, Type> CreateDefault(params Assembly[] assemblies)
        {
            var assemblySet = new HashSet<Assembly>(assemblies){
                Assembly.GetCallingAssembly(),
                Assembly.GetExecutingAssembly(),
                typeof(IEntity).Assembly
            };

            return CreateFrom(assemblySet)
                    .WithType(typeof(List<>))
                    .WithType(typeof(Dictionary<,>));
        }
    }
}
