
using Vent.Registry;
/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun
namespace Vent
{
    public interface IMutation : IEntity
    {
        public long TimeStamp { get; set; }
    }
}
