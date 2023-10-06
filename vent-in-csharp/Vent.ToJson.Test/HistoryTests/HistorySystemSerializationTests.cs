using Vent.ToJson.Readers;

using static Vent.ToJson.Utf8JsonWriterExtensions;

namespace Vent.ToJson.Test.HistoryTests
{
    /// <summary>
    /// Mimics the HistorySystem test but then with added serialization
    /// </summary>
    [TestClass]
    public class HistorySystemSerializationTests
    {
        private static readonly Utf8JsonEntityReader _reader = new ();

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
