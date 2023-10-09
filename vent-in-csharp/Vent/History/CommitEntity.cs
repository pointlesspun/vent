using Vent.Registry;
/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun
namespace Vent.History
{
    public class CommitEntity : MutateEntity
    {
        public CommitEntity()
        {
        }

        public CommitEntity(IEntity ent, IEntity version) : base(ent, version)
        {
        }

        public override string ToString()
        {
            return new DateTime(TimeStamp).ToString("HH:ss:FF") + " Commit id:" + MutatedEntityId;
        }

    }
}
