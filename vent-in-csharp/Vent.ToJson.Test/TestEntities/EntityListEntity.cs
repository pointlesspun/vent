namespace Vent.ToJson.Test.TestEntities
{

    public class EntityListEntity : EntityBase
    {
        public List<IEntity> EntityList
        {
            get;
            set;
        }

        public override object Clone()
        {
            var clone = (EntityListEntity)base.Clone();

            clone.EntityList = new List<IEntity>(EntityList.Select(e => e.Clone()).Cast<IEntity>());

            return clone;
        }

        public override bool Equals(object obj)
        {
            return obj is EntityListEntity entity
                   && Id == entity.Id
                   && (EntityList == null && entity.EntityList == null
                    || EntityList.SequenceEqual(entity.EntityList));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }


}
