/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using Vent.Registry;

namespace Vent.History
{
    public class EndMutationGroup : EntityBase, IMutation
    {
        public long TimeStamp { get; set; }

        public EndMutationGroup() { }

        public EndMutationGroup(long timeStamp)
        {
            TimeStamp = timeStamp;
        }

        public override string ToString()
        {
            return new DateTime(TimeStamp).ToString("HH:ss:FF") + " End group";
        }

    }
}
