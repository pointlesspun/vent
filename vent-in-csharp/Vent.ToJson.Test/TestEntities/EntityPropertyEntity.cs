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

            if (obj != null && obj is EntityPropertyEntity otherProperty)
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