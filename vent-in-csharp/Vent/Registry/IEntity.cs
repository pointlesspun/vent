/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun
namespace Vent.Registry
{
    public interface IEntity : ICloneable
    {
        /// <summary>
        /// Id of the entity. This can be used to find the entity in a EntityStore.
        /// If set to -1 the entity is not registered with a store
        /// </summary>
        int Id { get; set; }
    }
}