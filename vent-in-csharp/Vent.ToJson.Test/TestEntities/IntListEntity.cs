/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using Vent.Registry;

namespace Vent.ToJson.Test.TestEntities
{
    public class IntListEntity : EntityBase
    {
        public List<int> IntList
        {
            get;
            set;
        }

        public override object Clone()
        {
            var clone = (IntListEntity)base.Clone();

            clone.IntList = new List<int>(IntList);

            return clone;
        }

        public override bool Equals(object obj)
        {
            return obj is IntListEntity entity
                   && Id == entity.Id
                   && (IntList == null && entity.IntList == null
                    || IntList.SequenceEqual(entity.IntList));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }


}
