namespace Vent.ToJson.Test.TestEntities
{
    /// <summary>
    /// Entity containing a reference to another entity
    /// </summary>
    public class EntityPropertyEntity : EntityBase
    {
        public IEntity Other { get; set; }

        public EntityPropertyEntity() { }

        public EntityPropertyEntity(IEntity other)
        {
            Other = other;
        }

        public override bool Equals(object obj)
        {
            if (obj == this) return true;

            return obj != null
                && obj is EntityPropertyEntity other
                && (other.Other == null && Other == null
                    || other.Other.Id == Other.Id);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Other);
        }
    }
}