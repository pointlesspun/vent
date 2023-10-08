/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

namespace Vent
{
    public class MutateEntity : EntityBase, IMutation
    {
        public long TimeStamp { get; set; }

        /// <summary>
        /// Original id of the entity that is mutated. We
        /// keep this id to guarantee the id will be 
        /// the same before and after the mutation
        /// </summary>
        public int MutatedEntityId { get; set; }

        /// <summary>
        /// Reference to the head version. We want to be able to change the current
        /// working version as actions are being undone and redone. This means
        /// the entity you're holding can change as a result.
        /// </summary>
        public IEntity MutatedEntity { get; set; }

        /// <summary>
        /// The version of the entity captured during this mutation
        /// </summary>
        public IEntity AssociatedVersion { get; set; }

        public MutateEntity() 
        { 
        }

        public MutateEntity(IEntity ent, IEntity associatedVersion)
        {
            MutatedEntityId = ent.Id;
            MutatedEntity = ent;
            TimeStamp = DateTime.Now.Ticks;
            AssociatedVersion = associatedVersion;
        }
    }
}
