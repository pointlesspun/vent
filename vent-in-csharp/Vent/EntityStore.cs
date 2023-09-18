/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Collections;
using System.Data;

namespace Vent
{
    /// <summary>
    /// Container for entities representing (a portion) of the application state. 
    /// Entities may have version information associated with them which allows moving the global
    /// application state back in time (undo) or vice versa (redo).
    /// </summary>
    public class EntityStore : IEnumerable<KeyValuePair<int, IEntity>>
    {
        private int _entityId = 0;

        private readonly Dictionary<int, IEntity> _entities = new();

        /// <summary>
        /// Associates an entity by id with its versionInfo
        /// </summary>
        private readonly Dictionary<int, VersionInfo> _entityVersionInfo = new();

        private readonly List<IMutation> _mutations = new();

        private int _currentMutation = 0;

        public string Version { get; set; } = "0.2";

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
        /// Returns an enumertion of all mutations in this store
        /// </summary>
        public IEnumerable<IMutation> Mutations => _mutations;

        /// <summary>
        /// Returns the current active mutation ranges from -1 to MutationCount 
        /// </summary>
        public int CurrentMutation => _currentMutation;

        /// <summary>
        /// Number of mutations in this store
        /// </summary>
        public int MutationCount => _mutations.Count;

        /// <summary>
        /// Number of begin calls that have not yet got a corresponding end call
        /// </summary>
        public int OpenGroupCount { get; private set; } = 0;

        /// <summary>
        /// Maximum number of mutations, new mutations will cause the oldest
        /// mutation to be removed.
        /// </summary>
        public int MaxMutations
        {
            get;
            set;
        } = 10;

        public int MaxEntitySlots
        {
            get;
            set;
        } = int.MaxValue;

        /// <summary>
        /// Returns the Nth mutation applied to this store
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IMutation GetMutation(int index) => _mutations[index];

        /// <summary>
        /// If an entity version goes out of scope because the max mutations
        /// have been reached and the deleted mutation deletes the version information,
        /// its versioninfo may be preserved or deleted (depending on intended future use).
        /// If DeleteOutOfScopeVersions is true the versionInfo will be removed, if
        /// not it will remain in the store.
        /// </summary>
        public bool DeleteOutOfScopeVersions
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Returns the id of the next committed entity IF that entity
        /// is not yet in the store.
        /// </summary>
        public int NextEntityId => _entityId;

        /// <summary>
        /// Check if the given entity maintains versioninfo
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool HasVersionInfo(IEntity entity) => 
            entity != null && _entityVersionInfo.ContainsKey(entity.Id);

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
        public T Register<T>(T entity) where T : IEntity
        {
            // test preconditions
            Contract.Requires<ArgumentException>(entity != null, "Cannot register a null entity.");
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

        public void RestoreEntity(IEntity entity, int id)
        {
            Contract.Requires<ArgumentException>(id >= 0 && id < MaxEntitySlots, $"cannot restore an entity with an id ({id}) outside the valid id range ( 0..{MaxEntitySlots})");
            Contract.Requires<InvalidOperationException>(!_entities.ContainsKey(id), $"Trying to restore an entity in a slot ({id}) that is already occupied.");

            _entities[id] = entity;
            entity.Id = id;
        }

        public void RestoreNextEntityId(int id)
        {
            Contract.Requires<ArgumentException>(id >= 0 && id < MaxEntitySlots, $"cannot set an id ({id}) outside the valid id range ( 0..{MaxEntitySlots})");
            _entityId = id;
        }

        public void RestoreTransientProperties()
        {
            throw new NotImplementedException();
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

            if (_entityVersionInfo.TryGetValue(entity.Id, out VersionInfo versionInfo))
            {
                // remove all mutations in front of this one - if any
                TryRemoveFutureMutations();

                // entity must still exist after removing all mutations, if not the deregister
                // will be ignored - it basically counts as a NOP
                if (Contains(entity))
                {
                    var lastVersion = Register((IEntity)entity.Clone());
                    versionInfo.CommitVersion(lastVersion);

                    AddMutation(new DeregisterEntity(entity, lastVersion));

                    // keep this slot reserved
                    _entities[entity.Id] = null;
                }
            }
            else
            {
                _entities.Remove(entity.Id);
            }
            
            entity.Id = -1;
        }

        /// <summary>
        /// Returns the version info associated with this entity or null
        /// if no version info is associated with this entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public VersionInfo GetVersionInfo(IEntity entity) => 
            entity != null ? GetVersionInfo(entity.Id) : null;

        /// <summary>
        /// Returns the version info associated with this entityid or null
        /// if no version info is associated with this entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public VersionInfo GetVersionInfo(int id) =>
            _entityVersionInfo.TryGetValue(id, out VersionInfo value) ? value : null;


        /// <summary>
        /// Commits the given entity's state to the store. If the entity has not been 
        /// registered before or has no version info, version info will be added.
        /// 
        /// If the store is behind the head of the mutation list, all future mutations
        /// from the current mutation point will be removed.
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public T Commit<T>(T entity) where T : IEntity
        {
            Contract.Requires<ArgumentException>(entity != null);

            // remove all mutations in front of this one - if any
            TryRemoveFutureMutations();

            IEntity newVersion;

            // does this entity have version info
            if (_entityVersionInfo.TryGetValue(entity.Id, out VersionInfo versionInfo))
            {
                // entity may have been deregistered due to an undo 
                if (entity.Id < 0)
                {
                    // restore the entities' old id
                    entity.Id = versionInfo.HeadId;
                }

                // create a snapshot of the current entity
                newVersion = Register((IEntity)entity.Clone());
                versionInfo.CommitVersion(newVersion);
            }
            else
            {
                // entity is not yet added to the store?
                if (!Contains(entity))
                {
                    Register(entity);
                }

                newVersion = AddVersioning(entity);
            }

            // need to add the mutation last as this may clean up
            // existing mutations if maxMutations are exceeded. This
            // cleanup needs all previous information
            AddMutation(new CommitEntity(entity, newVersion));

            return entity;
        }

        /// <summary>
        /// Reverts the given entity to its last known version without changing
        /// the store's current mutation.
        /// </summary>
        /// <remarks>Requires the entity to be contained in this store.<see cref="EntityStore.Contains(IEntity)"/></remarks>
        /// <remarks>Requires the entity to have version information.</remarks>

        /// <param name="entity"></param>
        public void Revert(IEntity entity)
        {
            Contract.Requires<InvalidOperationException>(Contains(entity));

            var versionInfo = GetVersionInfo(entity);

            Contract.Requires<InvalidOperationException>(versionInfo != null, "Trying to revert an entity without versioning information");

            versionInfo?.Revert(entity);
        }

        /// <summary>
        /// Starts a group of mutations. All the mutations added after this
        /// until EndMutationGroup is called will be part of this group.
        /// When undo/redo is called, all entities in this group will be
        /// undone/redone.
        /// 
        /// Note that the group size cannot exceed MaxMutations. There
        /// is no explicit runtime test for this until an undo / redo
        /// is being called at which point a contract exception may be raised.
        /// </summary>
        public void BeginMutationGroup()
        {
            OpenGroupCount++;

            // remove all mutations in front of this one - if any
            TryRemoveFutureMutations();

            AddMutation(new BeginMutationGroup(DateTime.Now.Ticks));
        }

        /// <summary>
        /// End the current group. Removes any future mutations. 
        /// </summary>
        public void EndMutationGroup()
        {
            Contract.Requires<InvalidOperationException>(OpenGroupCount > 0, "Calling 'EndMutationGroup' without corresponding BeginMutationGroup.");

            // remove all mutations in front of this one - if any
            TryRemoveFutureMutations();

            AddMutation(new EndMutationGroup(DateTime.Now.Ticks));

            OpenGroupCount--;
        }

        /// <summary>
        /// Go back one mutation, then undo the action associated with this mutation.
        /// </summary>
        /// <returns></returns>
        public bool Undo()
        {
            Contract.Requires<InvalidOperationException>(OpenGroupCount == 0, $"Cannot undo while there are still open groups {OpenGroupCount}");
            
            var groupCount = 0;
            
            do
            {
                if (_currentMutation >= 0)
                {
                    // check if there's an entity which has moved out of scope and has to 
                    // be removed from the entity map.
                    TestPreviousEntityScope();

                    // go to the previous mutation
                    _currentMutation--;

                    if (_currentMutation >= 0)
                    {
                        var deltaGroupCount = UndoMutation(_mutations[_currentMutation]);
                        groupCount += deltaGroupCount;
                        OpenGroupCount += deltaGroupCount;
                    }
                    // if there are still open groups raise an exception as the
                    // group size exceeds MaxMutations
                    else if (groupCount > 0)
                    {
                        throw new InvalidOperationException("Group has no beginning. The group may have been larger than MaxMutations");
                    }
                }
            } while (groupCount > 0 && _currentMutation >= 0);

            return _currentMutation >= 0;
        }
        
        /// <summary>
        /// Redo the action of the current mutation, then move on to the next mutation.
        /// </summary>
        /// <returns></returns>
        public bool Redo()
        {
            var groupCount = 0;

            // move to the next position but can't redo anything since there's no
            // mutation at -1 
            if (_currentMutation < 0)
            {
                _currentMutation = 0;
                return true;
            }

            do
            {
                if (_currentMutation < _mutations.Count)
                {
                    var deltaGroupCount = RedoMutation(_mutations[_currentMutation]);

                    OpenGroupCount += deltaGroupCount;
                    groupCount += deltaGroupCount;

                    _currentMutation++;
                }
                else if (groupCount > 0)
                {
                    throw new InvalidOperationException("Group has no end. The mutation group may not have been properly closed before calling undo.");
                }
                else
                {
                    return false;
                }
            } while (groupCount > 0);

            return true;
        }

        public IEnumerator<KeyValuePair<int,IEntity>> GetEnumerator()
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

        public void DeleteMutation(int index)
        {
            Contract.Requires<ArgumentException>(index >= 0 && index < _mutations.Count, 
                $"DeleteMutation requires a valid index. Index provided is {index}, the index should be between 0 and {_mutations.Count}.");

            var deletedMutations = 0;

            switch (_mutations[index])
            {
                case BeginMutationGroup _:
                    deletedMutations = DeleteBeginMutationGroup(index);
                    break;
                    
                case MutateEntity _:
                    DeleteMutateEntityMutation(index);
                    deletedMutations = 1;
                    break;

                case EndMutationGroup _:
                    throw new InvalidOperationException("Cannot delete an EndMutationGroup, delete its corresponding BeginMutationGroup instead.");

                default:
                    throw new NotImplementedException($"No backing code to delete mutation of type: {_mutations[0].GetType().Name}");
            }

            if (_currentMutation > index)
            {
                _currentMutation -= deletedMutations;
            }
        }

        public bool IsGroupOpen(int mutationIndex)
        {
            Contract.Requires(_mutations[mutationIndex] is BeginMutationGroup);

            var groupCount = 0;

            do
            {
                switch (_mutations[mutationIndex])
                {
                    case BeginMutationGroup _:
                        groupCount++;
                        break;
                    case EndMutationGroup _:
                        groupCount--;
                        break;
                }

                mutationIndex++;

            } while (groupCount > 0 && mutationIndex < _mutations.Count);

            return groupCount > 0;
        }

        #region --- private methods -----------------------------------------------------------------------------------

        /// <summary>
        /// Tests if the entity associated with the current mutation is a commit and is the last version.
        /// In case of an undo, the entity is considered out of scope. 
        /// </summary>
        /// <param name="mutationIndex"></param>
        /// <returns></returns>
        private bool WillCurrentMutatedEntityGoOutOfScopeOnUndo(int mutationIndex)
        {
            if (mutationIndex < _mutations.Count)
            {
                var mutation = _mutations[mutationIndex];

                if (mutation is CommitEntity commit)
                {
                    var versionInfo = _entityVersionInfo[commit.MutatedEntityId];

                    return versionInfo.CurrentVersion == 0;
                }
            }

            return false;
        }

        /// <summary>
        /// Remove the entity associated with the mutation from the entity table.
        /// Also set its id to -1. Do not clean up its versioninfo as this call 
        /// is the result of an undo or redo and we need to preserve that versionInfo
        /// for future reverse undo / redo.
        /// </summary>
        /// <param name="mutationIndex"></param>
        private void RemoveCurrentMutatedEntityFromScope(int mutationIndex)
        {
            Contract.Requires(_mutations[mutationIndex] is MutateEntity);

            var mutation = _mutations[mutationIndex] as MutateEntity;
            var versionInfo = _entityVersionInfo[mutation.MutatedEntityId];
    
            versionInfo.CurrentVersion = -1;
            mutation.MutatedEntity.Id = -1;

            // keep the slot occupied
            _entities[mutation.MutatedEntityId] = null;
        }

        private IEntity ResolveMutatedEntity(MutateEntity mutation, VersionInfo versionInfo)
        {
            if (mutation.MutatedEntity != null)
            {
                // session contains the mutated version
                return mutation.MutatedEntity;
            }
            else if (_entities.TryGetValue(mutation.MutatedEntityId, out var ent))
            {
                // mutated version has left scope but was still in the store
                return ent;
            }
            else
            {
                // entity may have left the scope, create clone a new version
                var baseVersion = (IEntity) versionInfo.Versions[0].Clone();
                baseVersion.Id = mutation.MutatedEntityId;
                _entities[mutation.MutatedEntityId] = baseVersion;
                return baseVersion;
            }
        }

        private void AddMutation(IMutation mutation)
        {
            _mutations.Add(Register(mutation));
            _currentMutation++;

            // if max mutations <= 0 there is no limit to the number of mutations
            // one is able to execute
            if (MaxMutations > 0 && _mutations.Count > MaxMutations)
            {
                DeleteMutation(0);
            }
        }

        private void DeleteMutateEntityMutation(int index)
        {
            var mutation = (MutateEntity) _mutations[index];

            var versionInfo = _entityVersionInfo[mutation.MutatedEntityId];

            // remove the mutation and entity version from the entity store
            _entities.Remove(mutation.Id);
            _entities.Remove(mutation.AssociatedVersion.Id);

            // remove the version from the entity's version records
            versionInfo.RemoveVersion(mutation.AssociatedVersion);

            _mutations.RemoveAt(index);

            IEntity headEntity;

            if (DeleteOutOfScopeVersions && versionInfo.Versions.Count == 0)
            {
                headEntity = _entities[versionInfo.HeadId];

                // entity may already have been removed and the slot was just occupied,
                // so the head is null
                if (headEntity != null)
                {
                    headEntity.Id = -1;
                }

                _entityVersionInfo.Remove(versionInfo.HeadId);
                _entities.Remove(versionInfo.Id);
                _entities.Remove(versionInfo.HeadId);
            }
            
            // check if the head entity needs to be added to the registry again
            // only in case the deleted mutation is a deregister and the mutation
            // of that entity before that is a commit
            if (versionInfo.Versions.Count > 0
                && versionInfo.CurrentVersion >= 0
                && mutation is DeregisterEntity deregisterEntity)
            {
                if (!_entities.TryGetValue(versionInfo.HeadId, out headEntity) || headEntity == null)
                {
                    headEntity = deregisterEntity.MutatedEntity;
                    versionInfo.Revert(headEntity);
                    _entities[headEntity.Id] = headEntity;
                }
            }
        }       
        

        private void TryRemoveFutureMutations()
        {
            // are we at the head? if so ignore
            if (_currentMutation < _mutations.Count)
            {
                // adjust for the mutation being at the tail (index = -1)
                if (_currentMutation < 0)
                {
                    _currentMutation = 0;
                }

                // remove future mutations in the direction from head to tail.
                for (var i = _mutations.Count - 1; i >= _currentMutation; i--)  
                {
                    var mutation = _mutations[i];

                    if (mutation is BeginMutationGroup || mutation is EndMutationGroup)
                    {
                        // simply remove them, the rest of the group will be removed eventually
                        // in this loop
                        _mutations.RemoveAt(i);
                        _entities.Remove(mutation.Id);
                    }
                    else if (mutation is MutateEntity)
                    {
                        DeleteMutateEntityMutation(i);
                    }
                }
            }
        }

        /// <summary>
        /// Adds new versioning for the given entity and returns version 0
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private IEntity AddVersioning(IEntity entity)
        {
            var versionInfo = Register(new VersionInfo()
            {
                HeadId = entity.Id,
                Id = _entityId,
                CurrentVersion = 0,
            });

            // remove all mutations in front of this one - if any
            var version0 = Register((IEntity)entity.Clone());
            versionInfo.CommitVersion(version0);

            _entityVersionInfo[entity.Id] = versionInfo;

            return version0;
        }

        private void UndoMutateEntity(MutateEntity mutation)
        {
            var versionInfo = _entityVersionInfo[mutation.MutatedEntityId];
            var entity = ResolveMutatedEntity(mutation, versionInfo);

            // undoing a deregister will move the entity back into scope
            // (ie into the entity registry)
            if (mutation is DeregisterEntity)
            {
                _entities[mutation.MutatedEntityId] = entity;
            }

            versionInfo.Undo(entity);
        }

        private void RedoMutateEntity(MutateEntity mutation)
        {
            var versionInfo = _entityVersionInfo[mutation.MutatedEntityId];
            var entity = ResolveMutatedEntity(mutation, versionInfo);

            versionInfo.Redo(entity);

            // redo deregister/commit
            if (mutation is DeregisterEntity)
            {
                // keep this slot reserved
                _entities[entity.Id] = null;
                entity.Id = -1;
            }
            else if (mutation is CommitEntity && !Contains(entity))
            {
                _entities[entity.Id] = entity;
            }
        }
        
        // if the current mutation is pointing at a commit
        // and the version is at index 0, the associated 
        // entity will go out of scope and should be removed
        // from the entity registry
        private void TestPreviousEntityScope()
        {
            if (WillCurrentMutatedEntityGoOutOfScopeOnUndo(_currentMutation))
            {
                RemoveCurrentMutatedEntityFromScope(_currentMutation);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private int UndoMutation(IMutation mutation) 
        {
            switch (mutation)
            {
                case BeginMutationGroup _:
                    return -1;
                case EndMutationGroup _:
                    return 1;
                case MutateEntity mutateEntity:
                    UndoMutateEntity(mutateEntity);
                    return 0;
                default:
                    throw new NotImplementedException($"Undo mutation of type {mutation.GetType().Name} has no backing code");
            }
        }

        private int RedoMutation(IMutation mutation)
        {
            switch (mutation)
            {
                case BeginMutationGroup _:
                    return 1;
                case EndMutationGroup _:
                    return -1;
                case MutateEntity mutateEntity:
                    RedoMutateEntity(mutateEntity);
                    return 0;
                default:
                    throw new NotImplementedException($"{mutation.GetType().Name} has no backing code");
            }
        }

        private int DeleteBeginMutationGroup(int mutationIndex)
        {
            Contract.Requires<InvalidOperationException>(!IsGroupOpen(mutationIndex), 
                $"Cannot remove the begin group mutation at index {mutationIndex} while it is still open.");

            var groupCount = 0;
            var deletedMutationCount = 0;

            do
            {
                var mutation = _mutations[mutationIndex];

                switch (mutation)
                {
                    case BeginMutationGroup _:
                        groupCount++;
                        _mutations.RemoveAt(mutationIndex);
                        _entities.Remove(mutation.Id);
                        break;
                    case EndMutationGroup _:
                        groupCount--;
                        _mutations.RemoveAt(mutationIndex);
                        _entities.Remove(mutation.Id);
                        break;
                    case MutateEntity _:
                        DeleteMutateEntityMutation(mutationIndex);
                        break;
                }

                deletedMutationCount++;


            } while (groupCount > 0);

            return deletedMutationCount;
        }

        #endregion
    }
}
