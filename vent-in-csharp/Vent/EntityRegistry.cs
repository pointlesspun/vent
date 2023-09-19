using System.Collections;

namespace Vent
{
    public class EntityRegistry : EntityBase, IEnumerable<KeyValuePair<int, IEntity>>
    {
        private int _entityId = 0;

        private Dictionary<int, IEntity> _entities = new();

        public virtual Dictionary<int, IEntity> Entities
        {
            get => _entities;
            set => _entities = value;
        }

        public int MaxEntitySlots
        {
            get;
            set;
        } = int.MaxValue;

        public int NextEntityId
        {
            get => _entityId;
            set => _entityId = value;
        }

        /// <summary>
        /// Number of entities in this store. Note this iterates over all 
        /// occupied entity slots so it might be rather slow.
        /// </summary>
        public int EntitiesInScope => _entities.Values.Where(e => e != null).Count();

        /// <summary>
        /// Number of slots in use
        /// </summary>
        public int SlotCount => _entities.Count;

        /// <summary>
        /// Returns the entity with the given id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The entity with the given id or null if no such entity exists</returns>
        public IEntity this[int id]
        {
            get
            {
                if (_entities.TryGetValue(id, out var entity))
                {
                    if (entity.Id >= 0)
                    {
                        return entity;
                    }
                    // else entity has been removed but still being tracked
                }

                return null;
            }
        }

        /// <summary>
        /// Register an entity without adding versioning
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns>the registered entity</returns>
        public T Register<T>(T entity) where T : class, IEntity
        {
            // test preconditions
            Contract.Requires<ArgumentException>(entity != null, "Cannot register a null entity.");
            Contract.Requires<ArgumentException>(entity != this, "Cannot register self.");
            Contract.Requires<InvalidOperationException>(!Contains(entity), "Cannot register an entity that is already in the store.");
            Contract.Requires<InvalidOperationException>(_entities.Count < MaxEntitySlots, "Cannot register an entity when the maxEntitySlots has been reached.");

            // wrap around 
            if (_entityId >= MaxEntitySlots)
            {
                _entityId = 0;
            }

            var attempts = 0;
            // find first available slot
            while (_entities.ContainsKey(_entityId))
            {
                _entityId++;

                if (_entityId >= MaxEntitySlots)
                {
                    _entityId = 0;
                }

                attempts++;

                if (attempts == MaxEntitySlots)
                {
                    throw new OutOfMemoryException("No more entity slots available");
                }
            }

            entity.Id = _entityId;
            _entityId++;
            _entities[entity.Id] = entity;

            return entity;
        }

        /// <summary>
        /// Deregisters the given entity and sets the id to -1. 
        /// If the entity has version information
        /// associated with it, this will add a deregister mutation to the store.
        /// If the store is behind the head of the mutation list, all future mutations
        /// from the current mutation point will be removed.
        /// </summary>
        /// <param name="entity"></param>
        public void Deregister(IEntity entity)
        {
            Contract.Requires<ArgumentException>(entity != null);
            Contract.Requires<InvalidOperationException>(Contains(entity));

            _entities.Remove(entity.Id);
            entity.Id = -1;
        }

        public void Link(IEntity entity)
        {
            Contract.Requires<ArgumentException>(entity != null);
            Contract.Requires<InvalidOperationException>(!Contains(entity));
            Contract.Requires<InvalidOperationException>(_entities.ContainsKey(entity.Id));
            Contract.Requires<ArgumentException>(entity != this, "Cannot register self.");

            if (_entities.TryGetValue(entity.Id, out var existingEntity))
            {
                if (existingEntity != null)
                {
                    existingEntity.Id = -1;
                }
            }

            _entities[entity.Id] = entity;
        }

        public void Unlink(IEntity entity)
        {
            Contract.Requires<ArgumentException>(entity != null);
            Contract.Requires<InvalidOperationException>(Contains(entity));

            _entities[entity.Id] = null;
            entity.Id = -1;
        }

        public IEnumerator<KeyValuePair<int, IEntity>> GetEnumerator()
        {
            return _entities.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _entities.Values.GetEnumerator();
        }

        /// <summary>
        /// Checks if this entity object is in this store
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool Contains(IEntity entity)
        {
            return entity != null
                && entity.Id >= 0
                && _entities.TryGetValue(entity.Id, out IEntity other)
                && other == entity;
        }

        public override object Clone()
        {
            var result = (EntityRegistry) base.Clone();

            result.Entities = new Dictionary<int, IEntity>(); 

            foreach (var kvp in _entities) 
            {
                result.Entities[kvp.Key] = (IEntity) kvp.Value.Clone();
            }

            return result;
        }
    }
}
