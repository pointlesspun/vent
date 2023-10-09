/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using Vent.Registry;

namespace Vent.History
{
    public class DeregisterEntity : MutateEntity
    {
        public DeregisterEntity()
        {
        }

        public DeregisterEntity(IEntity ent, IEntity version) : base(ent, version)
        {
        }

        public override string ToString()
        {
            return new DateTime(TimeStamp).ToString("HH:ss:FF") + " deregister id:" + MutatedEntityId;
        }
    }
}
