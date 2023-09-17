/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun


namespace Vent
{
    public class BeginMutationGroup : EntityBase, IMutation
    {
        public long TimeStamp { get; set; }

        public BeginMutationGroup() { }

        public BeginMutationGroup(long timeStamp) 
        { 
            TimeStamp = timeStamp;  
        }


        public override string ToString()
        {
            return new DateTime(TimeStamp).ToString("HH:ss:FF") + " Begin group";
        }

    }
}
