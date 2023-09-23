using System.Collections;

namespace Vent
{
    public class EntityRegistry : EntityBase, IEnumerable<KeyValuePair<int, IEntity>>
    {
        private int _entityId = 0;

        private Dictionary<int, IEntity> _entities = new();

        public static readonly int Unregistered = -1;

        public virtual Dictionary<int, IEntity> EntitySlots
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
                    if (entity != null && entity.Id >= 0)
                    {
                        return entity;
                    }
                    // else entity has been removed but still being tracked
                }

                return null;
            }
        }

        /// <summary>
        /// Adds an entity and assigns it to a slot, changing its id.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns>the added entity</returns>
        public T Add<T>(T entity) where T : class, IEntity
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
        /// Removes the entity (if any) from the slot with the given id. If an entity is occupying
        /// this slot, it will have its id set to Unregistered.
        /// </summary>
        /// <param name="entity"></param>
        public void Remove(int id)
        {
            if (_entities.TryGetValue(id, out var entity))
            {
                _entities.Remove(id);

                // slot may just have been occupied
                if (entity != null)
                {
                    entity.Id = Unregistered;
                }
            }
            else
            {
                // entities did not contain id            
                Contract.Requires<ArgumentException>(false);
            }
        }

        /// <summary>
        /// Assign the given entity to the given slot. If an existing entity 
        /// was occupying the slot, it will be removed and its id set to Unregistered.
        /// If the slot is not occupied, the entity will be simply assigned.
        /// </summary>
        /// <param name="entity"></param>
        public T SetSlot<T>(int slotId, T entity) where T : class, IEntity
        {
            Contract.Requires<ArgumentException>(entity != null, "Entity cannot be null.");
            Contract.Requires<ArgumentException>(entity != this, "Cannot register self.");
            Contract.Requires<InvalidOperationException>(!Contains(entity));
            
            if (_entities.TryGetValue(slotId, out var existingEntity))
            {
                if (existingEntity != null)
                {
                    existingEntity.Id = Unregistered;
                }
            }

            _entities[slotId] = entity;
            entity.Id = slotId;

            return entity;
        }

        public void SetSlotToNull(int slotId)
        {
            if (_entities.TryGetValue(slotId, out var existingEntity))
            {
                if (existingEntity != null)
                {
                    existingEntity.Id = Unregistered;
                }
            }

            _entities[slotId] = null;
        }

        /// <summary>
        /// Clears the occupied slot by setting its value to 0. This does not
        /// change the SlotCount. If any entity was occupying this slot, its id will
        /// now be set to Unregistered.
        /// </summary>
        /// <param name="id"></param>
        public void ClearSlot(int id)
        {
            Contract.Requires<ArgumentException>(id >= 0, $"Cannot remove entity from slot using id {id}.");
            
            if (_entities.TryGetValue(id, out var entity))
            {
                Contract.Requires<InvalidOperationException>(entity != null, $"Trying to remove entity from slot at {id}, but there was no entity.");
                _entities[id] = null;
                entity.Id = Unregistered;
            }
            else
            {
                // entities did not contain id            
                Contract.Requires<ArgumentException>(false);
            }
        }

        public IEnumerable<T> GetEntitiesOf<T>() where T : class, IEntity => 
            _entities.Values.Where(e => e is T).Cast<T>();
        
        public IEnumerator<KeyValuePair<int, IEntity>> GetEnumerator() => 
            _entities.GetEnumerator();
        
        IEnumerator IEnumerable.GetEnumerator() => 
            _entities.Values.GetEnumerator();
        
        /// <summary>
        /// Checks if this entity object is in this store
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool Contains(IEntity entity) =>
                entity != null
                && entity.Id >= 0
                && _entities.TryGetValue(entity.Id, out IEntity other)
                && other == entity;
        
        public bool ContainsKey(int key) => 
            _entities.ContainsKey(key);
        

        public override object Clone()
        {
            var result = (EntityRegistry) base.Clone();

            result.EntitySlots = new Dictionary<int, IEntity>(); 

            foreach (var kvp in _entities) 
            {
                result.EntitySlots[kvp.Key] = (IEntity) kvp.Value.Clone();
            }

            return result;
        }
    }
}
