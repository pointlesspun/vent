/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using Vent.Registry;

namespace Vent
{
    public interface IMutation : IEntity
    {
        public long TimeStamp { get; set; }
    }
}
