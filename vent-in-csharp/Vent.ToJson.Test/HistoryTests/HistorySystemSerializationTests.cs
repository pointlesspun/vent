using Microsoft.Win32;
using Vent.PropertyEntities;
using Vent.ToJson.Readers;

using static Vent.ToJson.Utf8JsonWriterExtensions;

namespace Vent.ToJson.Test.HistoryTests
{
    /// <summary>
    /// Mimics the HistorySystem test but then with added serialization. Essentially spot checks
    /// to see if serialization works as intended.
    /// </summary>
    [TestClass]
    public class HistorySystemSerializationTests
    {
        private static readonly Utf8JsonEntityReader _reader = new ();

        
        [TestMethod]
        public void TriggerDeleteDegisterWhenEntityOutOfScopeTest()
        {
            var registry = new EntityRegistry();
            var history = new HistorySystem(registry);
            var ent = history.Commit(new StringEntity("foo"));
            var entId = ent.Id;
            history.Deregister(ent);

            // de/serialize and continue
            var registry1 = CloneViaJson(registry);
            var history1 = CreateHistorySystemFromRegistry(registry1);

            // deleteing mutation 1, which is a deregister should bring 
            // ent1 back into existance 
            history1.DeleteMutation(1);

            var ent1Clone = registry1[entId] as StringEntity;
            Assert.IsTrue(ent1Clone.Value == "foo");       
        }

        // mirrors HistorySystemTest.RegisterDegisterPropertyEntityTest
        [TestMethod]
        public void RegisterDegisterPropertyEntityTest()
        {
            var registry = new EntityRegistry();
            var history = new HistorySystem(registry);

            var ent = registry.Add(new PropertyEntity<string>("foo"));

            var registryClone0 = CloneViaJson(registry);

            // count = ent + history data
            Assert.IsTrue(registryClone0.EntitiesInScope == 2);
            Assert.IsTrue(registryClone0[ent.Id].Equals(ent));
            
            var history0 = CreateHistorySystemFromRegistry(registryClone0);
            var entId = ent.Id;

            history.DeregisterById(entId);
            history0.DeregisterById(entId);

            // count = history data
            Assert.IsTrue(registry.EntitiesInScope == 1);
            Assert.IsTrue(CloneViaJson(registry).EntitiesInScope == 1);
            Assert.IsTrue(CloneViaJson(registryClone0).EntitiesInScope == 1);
        }

        // mirrors HistorySystemTest.RegisterDegisterVersionedPropertyEntityTest
        [TestMethod]
        public void RegisterDegisterVersionedPropertyEntityTest()
        {
            var registry = new EntityRegistry();
            var history = new HistorySystem(registry);
            var ent = history.Commit(new PropertyEntity<string>("foo"));

            // 1 for the entity, one for the version info 1 for 
            // the version0 and 1 for the mutations
            Assert.IsTrue(registry.EntitiesInScope == 5);
            Assert.IsTrue(history.Mutations.Count() == 1);

            var mutation = history.Mutations.ElementAt(0);

            Assert.IsTrue(mutation is CommitEntity);
            Assert.IsTrue(((CommitEntity)mutation).MutatedEntityId == ent.Id);

            Assert.IsTrue(registry[ent.Id] == ent);

            var versionInfo = history.GetVersionInfo(ent);

            Assert.IsTrue(versionInfo != null);
            Assert.IsTrue(versionInfo.Versions.Count == 1);

            // version 0 should be stored as a separate entity
            var version0 = versionInfo.Versions[0] as PropertyEntity<string>;
            Assert.IsTrue(version0 != ent);
            Assert.IsTrue(version0.Id != ent.Id);
            Assert.IsTrue(version0.Value == ent.Value);

            var oldEntityId = ent.Id;

            history.Deregister(ent);

            Assert.IsTrue(ent.Id == -1);

            // version info and version0 should still be there
            // a new mutation should be added
            // as well as a version1 for deregisting
            Assert.IsTrue(registry.EntitiesInScope == 6);
            Assert.IsTrue(registry.SlotCount == 7);
            Assert.IsTrue(history.Mutations.Count() == 2);

            // clone check, test the exact same values
            var registryClone0 = CloneViaJson(registry);
            var historyClone0 = CreateHistorySystemFromRegistry(registryClone0);

            Assert.IsTrue(registryClone0.EntitiesInScope == 6);
            Assert.IsTrue(registryClone0.SlotCount == 7);
            Assert.IsTrue(historyClone0.Mutations.Count() == 2);

            var commitMutation = historyClone0.Mutations.ElementAt(0) as CommitEntity;

            Assert.IsTrue(commitMutation != null);
            Assert.IsTrue(commitMutation.MutatedEntityId == oldEntityId);

            // ent0-head has can no longer be resolved as in the serialized version 
            // it is not in the registry
            var ent0 = registryClone0[oldEntityId];

            Assert.IsTrue(ent0 == null);
            Assert.IsTrue(commitMutation.MutatedEntity == ent0);

            var deregisterMutation = historyClone0.Mutations.ElementAt(1) as DeregisterEntity;

            Assert.IsTrue(deregisterMutation != null);
            Assert.IsTrue(deregisterMutation.MutatedEntityId == oldEntityId);
            Assert.IsTrue(deregisterMutation.MutatedEntity == ent0);

            var versionInfoClone0 = historyClone0.GetVersionInfo(oldEntityId);
            Assert.IsTrue(versionInfoClone0.Versions.Count == 2);

            // an undo should restore this version
            historyClone0.Undo();

            ent0 = registryClone0[oldEntityId];
            Assert.IsTrue(ent0 != null);

            // this should be restored
            Assert.IsTrue(deregisterMutation.MutatedEntity == ent0);

            // this should still be null
            Assert.IsTrue(commitMutation.MutatedEntity == null);

            // go back one more time
            historyClone0.Undo();

            // this first commit mutation should still has it's mutated entity restored
            Assert.IsTrue(commitMutation.MutatedEntity == ent0);
        }

        // mirrors HistorySystemTest.RegisterUndoTest
        [TestMethod]
        public void RegisterUndoTest()
        {
            var registry = new EntityRegistry();
            var store = new HistorySystem(registry);

            var ent = store.Commit(new PropertyEntity<string>()
            {
                Value = "foo"
            });
            var entId = ent.Id;

            ent.Value = "bar";
            store.Undo();

            var registryClone0 = CloneViaJson(registry);
            var historyClone0 = CreateHistorySystemFromRegistry(registryClone0);
            var ent0 = registryClone0[entId];

            Assert.IsTrue(((PropertyEntity<string>) ent0).Value == "foo");
            Assert.IsTrue(historyClone0.CurrentMutation == 0);
        }

        [TestMethod]
        public void RegisterCommitUndoTest()
        {
            var registry = new EntityRegistry();
            var history = new HistorySystem(registry);

            var ent = history.Commit(new PropertyEntity<string>()
            {
                Value = "foo"
            });
            ent.Value = "bar";

            history.Commit(ent);

            Assert.IsTrue(history.CurrentMutation == 2);

            var registry0 = CloneViaJson(registry);
            var history0 = CreateHistorySystemFromRegistry(registry0);
            var ent0 = (PropertyEntity<string>) registry0[ent.Id];

            history0.Undo();

            Assert.IsTrue(ent0.Value == "bar");
            Assert.IsTrue(history0.CurrentMutation == 1);

            history0.Undo();

            Assert.IsTrue(ent0.Value == "foo");
            Assert.IsTrue(history0.CurrentMutation == 0);
        }

        [TestMethod]
        public void RegisterCommitUndoRedoTest()
        {
            var registry = new EntityRegistry();
            var history = new HistorySystem(registry);

            var ent = history.Commit(new PropertyEntity<string>()
            {
                Value = "foo"
            });

            ent.Value = "bar";
            history.Commit(ent);

            ent.Value = "qez";
            history.Commit(ent);

            // go to the beginning, head is assumed to be ahead (different)
            // from the last commit so the first undo will result in the 
            // last commit
            history.Undo();
            history.Undo();
            history.Undo();

            // de/serialize and continue
            var registry0 = CloneViaJson(registry);
            var history0 = CreateHistorySystemFromRegistry(registry0);
            var ent0 = (PropertyEntity<string>)registry0[ent.Id];

            Assert.IsFalse(history0.Undo());

            // go to the head
            history0.Redo();
            Assert.IsTrue(ent0.Value == "foo");

            history0.Redo();
            Assert.IsTrue(ent0.Value == "foo");

            history0.Redo();
            Assert.IsTrue(ent0.Value == "bar");

            history0.Redo();
            Assert.IsTrue(ent0.Value == "qez");

            Assert.IsFalse(history0.Redo());

            // de/serialize and continue
            var registry1 = CloneViaJson(registry0);
            var history1 = CreateHistorySystemFromRegistry(registry1);
            var ent1 = (PropertyEntity<string>)registry1[ent.Id];

            // go half way back (head: nop, M = M2)
            history1.Undo();
            Assert.IsTrue(ent1.Value == "qez");

            // M2: ent = bar, M = M1
            history1.Undo();
            Assert.IsTrue(ent1.Value == "bar");

            // go to the end again
            history1.Redo();
            Assert.IsTrue(ent1.Value == "bar");

            history1.Redo();
            Assert.IsTrue(ent1.Value == "qez");

            Assert.IsFalse(history1.Redo());
        }

        // check if the mutations cut off after the max history count
        // has been reached
        [TestMethod]
        public void CutOffTest()
        {
            var registry = new EntityRegistry();
            var entityHistory = registry.Add(new EntityHistory(2));
            var history = new HistorySystem(registry, entityHistory);

            var ent = history.Commit(new PropertyEntity<string>("foo"));
            var values = new[] { "bar", "qez", "xim" };

            ent.Value = values[0];
            history.Commit(ent);

            ent.Value = values[1];
            history.Commit(ent);

            // de/serialize and continue
            var registry1 = CloneViaJson(registry);
            var history1 = CreateHistorySystemFromRegistry(registry1);
            var ent1 = (PropertyEntity<string>)registry1[ent.Id];

            ent1.Value = values[2];
            history1.Commit(ent1);

            Assert.IsTrue(history1.Mutations.Count() == 2);
            
            // head and two versions
            Assert.IsTrue(registry1.Where(kvp => kvp.Value is PropertyEntity<string>).Count() == 3);
            Assert.IsTrue(registry1.Where(kvp => kvp.Value is VersionInfo).Count() == 1);
        }

        // check if the mutations are overwritten when a commit takes 
        // place when the head is not at the tail
        [TestMethod]
        public void UndoAndCommitTest()
        {
            var registry = new EntityRegistry();
            var history = new HistorySystem(registry);
            var ent = history.Commit(new PropertyEntity<string>("foo"));
            var values = new[] { "bar", "qez", "xim" };

            foreach (var str in values)
            {
                ent.Value = str;
                history.Commit(ent);
            }

            history.Undo(3);

            Assert.IsTrue(ent.Value == "bar");

            ent.Value = "tun";
            history.Commit(ent);

            // de/serialize and continue
            var registry1 = CloneViaJson(registry);
            var history1 = CreateHistorySystemFromRegistry(registry1);
            var ent1 = (PropertyEntity<string>)registry1[ent.Id];

            // versions should be foo and tun
            Assert.IsTrue(history1.MutationCount == 2);

            // ent, 2 versions, versioninfo, 2 mutations
            Assert.IsTrue(registry1.EntitiesInScope == 7);

            Assert.IsFalse(history1.Redo());

            history1.Undo();
            history1.Undo();
            Assert.IsTrue(ent1.Value == "foo");

            // store is now at tail
            history1.Redo();
            Assert.IsTrue(ent1.Value == "foo");

            history1.Redo();
            Assert.IsTrue(ent1.Value == "tun");

            Assert.IsFalse(history1.Redo());
        }

        // simple back and forth with three entities
        [TestMethod]
        public void MutlipleEntityTest()
        {
            var registry = new EntityRegistry();
            var history = new HistorySystem(registry);
            var ent1 = history.Commit(new PropertyEntity<string>("foo"));
            var ent2 = history.Commit(new PropertyEntity<string>("bar"));
            var ent3 = history.Commit(new PropertyEntity<string>("qez"));

            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
            Assert.IsTrue(registry.Contains(ent3));

            // de/serialize and continue
            var registry1 = CloneViaJson(registry);
            var history1 = CreateHistorySystemFromRegistry(registry1);
            var ent1Clone1 = (PropertyEntity<string>)registry1[ent1.Id];
            var ent2Clone1 = (PropertyEntity<string>)registry1[ent2.Id];
            var ent3Clone1 = (PropertyEntity<string>)registry1[ent3.Id];

            history1.ToTail();

            Assert.IsFalse(registry1.Contains(ent1Clone1));
            Assert.IsFalse(registry1.Contains(ent2Clone1));
            Assert.IsFalse(registry1.Contains(ent3Clone1));

            // de/serialize and continue
            var registry2 = CloneViaJson(registry1);
            var history2 = CreateHistorySystemFromRegistry(registry2);
            
            history2.ToHead();

            var ent1Clone2 = (PropertyEntity<string>)registry2[ent1.Id];
            var ent2Clone2 = (PropertyEntity<string>)registry2[ent2.Id];
            var ent3Clone2 = (PropertyEntity<string>)registry2[ent3.Id];

            Assert.IsTrue(registry2.Contains(ent1Clone2));
            Assert.IsTrue(registry2.Contains(ent2Clone2));
            Assert.IsTrue(registry2.Contains(ent3Clone2));
        }

        [TestMethod]
        public void RemoveOldestCommitMutationTest()
        {
            var registry = new EntityRegistry();
            var history = new HistorySystem(registry);
            var ent1 = new PropertyEntity<string>("foo");
            var ent2 = new PropertyEntity<string>("bar");
            history.Commit(ent1);
            history.Commit(ent2);

            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
            Assert.IsTrue(history.MutationCount == 2);
            Assert.IsTrue(history.CurrentMutation == 2);
            Assert.IsTrue(registry.EntitiesInScope == 9);
            Assert.IsTrue(registry.SlotCount == 9);

            history.DeleteMutation(0);

            // de/serialize and continue
            var registry1 = CloneViaJson(registry);
            var history1 = CreateHistorySystemFromRegistry(registry1);
            var ent1Clone1 = (PropertyEntity<string>)registry1[ent1.Id];
            var ent2Clone1 = (PropertyEntity<string>)registry1[ent2.Id];

            Assert.IsFalse(registry1.Contains(ent1Clone1));
            Assert.IsTrue(registry1.Contains(ent2Clone1));
            Assert.IsTrue(history1.MutationCount == 1);
            Assert.IsTrue(history1.CurrentMutation == 1);
            Assert.IsTrue(registry1.EntitiesInScope == 5);
            Assert.IsTrue(registry1.SlotCount == 5);

            history1.DeleteMutation(0);

            Assert.IsFalse(registry1.Contains(ent1Clone1));
            Assert.IsFalse(registry1.Contains(ent2Clone1));
            Assert.IsTrue(history1.MutationCount == 0);
            Assert.IsTrue(history1.CurrentMutation == 0);
            Assert.IsTrue(registry1.EntitiesInScope == 1);
            Assert.IsTrue(registry1.SlotCount == 1);
        }

        /// <summary>
        /// Remove the middle deregister after going back to the tail, see if everything is peachy. This
        /// should NOT bring back the entity which was deleted because the entity
        /// is not in scope yet
        /// </summary>
        [TestMethod]
        public void RemoveDeregisterInMiddlePositionAfterGoToTailTest()
        {
            var registry = new EntityRegistry();
            var history = new HistorySystem(registry);
            var ent1 = history.Commit(new PropertyEntity<string>("foo"));
            history.Commit(ent1.With("bar"));
            var ent1Id = ent1.Id;

            history.Deregister(ent1);
            var ent2 = history.Commit(new PropertyEntity<string>("qaz"));
            var ent2Id = ent2.Id;

            // history = 1
            // ent1 initial commit "foo" = 5
            // commit "bar" = +2 (7)
            // deregister = +2, -1 (8)
            // ent2 = +=4 (12)
            Assert.IsTrue(registry.EntitiesInScope == 12);

            // verify pre-state
            history.ToTail();

            // should have deregistered ent2 so entities in scope should be 11            
            Assert.IsTrue(registry.EntitiesInScope == 11);
            Assert.IsTrue(history.CurrentMutation == -1);
            Assert.IsTrue(history.MutationCount == 4);
            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsFalse(registry.Contains(ent2));

            // de/serialize and continue
            var registry1 = CloneViaJson(registry);
            var history1 = CreateHistorySystemFromRegistry(registry1);

            // remove the deregister 
            history1.DeleteMutation(2);
            

            // test outcome
            Assert.IsTrue(history1.CurrentMutation == -1);
            Assert.IsTrue(history1.MutationCount == 3);

            Assert.IsFalse(registry1.Contains(ent1));
            Assert.IsFalse(registry1.Contains(ent2));
            Assert.IsTrue(registry1.EntitiesInScope == 9);

            history1.ToHead();

            Assert.IsTrue(registry1.ContainsKey(ent1Id));
            Assert.IsTrue(registry1.ContainsKey(ent2Id));
            Assert.IsTrue(((PropertyEntity<string>)registry1[ent1Id]).Value == "bar");
        }

        private static T CloneViaJson<T>(T entity) where T : IEntity
        {
            var entityString = WriteObjectToJsonString(entity, EntitySerialization.AsValue);
            return (T)_reader.ReadFromJson(entityString, entitySerialization: EntitySerialization.AsValue);
        }

        private static HistorySystem CreateHistorySystemFromRegistry(EntityRegistry registry)
        {
            return new HistorySystem(registry, (EntityHistory)registry.First(e => e.Value is EntityHistory).Value);
        }
    }
}
