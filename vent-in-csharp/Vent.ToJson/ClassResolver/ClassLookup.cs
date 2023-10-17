/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Reflection;
using Vent.Registry;
using Vent.Util;

namespace Vent.ToJson.ClassResolver
{
    /// <summary>
    /// Methods to create a collection of declared classes from which entities can be deserialized.
    /// </summary>
    public static class ClassLookup
    {
        /// <summary>
        /// Create a classlookup based on the public non static types declared in the given assemblies
        /// </summary>
        /// <param name="assemblies"></param>
        /// <returns>A dictionary with the full class name as key and the associated type as value.</returns>
        public static Dictionary<string, Type> CreateFrom(params Assembly[] assemblies)
        {
            Contract.NotNull(assemblies);

            return CreateFrom((IEnumerable<Assembly>)assemblies);
        }

        /// <summary>
        /// Create a classlookup based on the given types
        /// </summary>
        /// <param name="types"></param>
        /// <returns>A dictionary with the full class name as key and the associated type as value.</returns>
        public static Dictionary<string, Type> CreateFrom(params Type[] types)
        {
            Contract.NotNull(types);

            var classLookup = new Dictionary<string, Type>();

            foreach (var type in types)
            {
                classLookup[type.ToVentClassName()] = type;
            }

            return classLookup;
        }
        /// <summary>
        /// Create a classlookup based on the public non static types declared in the given assemblies
        /// </summary>
        /// <param name="assemblies"></param>
        /// <returns>A dictionary with the full class name as key and the associated type as value.</returns>
        public static Dictionary<string, Type> CreateFrom(IEnumerable<Assembly> assemblies)
        {
            Contract.NotNull(assemblies);

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

        /// <summary>
        /// Adds the given type to the classlookup
        /// </summary>
        /// <param name="classLookup"></param>
        /// <param name="type"></param>
        /// <returns>The parameter classlookup</returns>
        public static Dictionary<string, Type> WithType(this Dictionary<string, Type> classLookup, Type type)
        {
            classLookup[type.ToVentClassName()] = type;
            return classLookup;
        }

        /// <summary>
        /// Creates a default classlookup from the given assemblies, the calling and executing assembly
        /// as well as the Vent asembly.
        /// </summary>
        /// <param name="assemblies"></param>
        /// <returns>A dictionary with the full class name as key and the associated type as value.</returns>
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
