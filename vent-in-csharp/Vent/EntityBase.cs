/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

namespace Vent
{
    public class EntityBase : IEntity
    {
        public int Id { get; set; } = -1;
        
        public virtual object Clone()
        {
            return MemberwiseClone();
        }
    }
}
