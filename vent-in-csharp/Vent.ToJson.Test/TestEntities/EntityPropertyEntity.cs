namespace Vent.ToJson.Test.TestEntities
{
    /// <summary>
    /// Entity containing a reference to another entity
    /// </summary>
    public class EntityReferenceEntity : EntityBase
    {
        public IEntity Other { get; set; }

        public EntityReferenceEntity() { }

        public EntityReferenceEntity(IEntity other)
        {
            Other = other;
        }

        public override bool Equals(object obj)
        {
            if (obj == this) return true;

            if (obj != null && obj is EntityReferenceEntity otherProperty)
            {
                return otherProperty.Other == null && Other == null
                     || (otherProperty.Other.Id == Other.Id
                             && otherProperty.Other.Equals(Other));
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return Other == null ? "-> null" : $"-> @{Other.Id}: {Other}";
        }
    }
}