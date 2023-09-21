/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

namespace Vent.Test
{
    [TestClass]
    public class EntityStoreTest
    {
        [TestMethod]
        public void RegisterDegisterPropertyEntityTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);

            var ent = registry.Register(new PropertyEntity<string>("foo"));

            Assert.IsTrue(registry.EntitiesInScope == 1);
            Assert.IsTrue(registry[ent.Id] == ent);

            store.Deregister(ent);

            Assert.IsTrue(registry.EntitiesInScope == 0);
        }

        [TestMethod]
        public void RegisterDegisterVersionedPropertyEntityTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent = store.Commit(new PropertyEntity<string>("foo"));

            // 1 for the entity, one for the version info 1 for 
            // the version0 and 1 for the mutations
            Assert.IsTrue(registry.EntitiesInScope == 4);

            Assert.IsTrue(store.Mutations.Count() == 1);

            var mutation = store.Mutations.ElementAt(0);

            Assert.IsTrue(mutation is CommitEntity);
            Assert.IsTrue(((CommitEntity)mutation).MutatedEntityId == ent.Id);

            Assert.IsTrue(registry[ent.Id] == ent);

            var versionInfo = store.GetVersionInfo(ent);

            Assert.IsTrue(versionInfo != null);
            Assert.IsTrue(versionInfo.Versions.Count == 1);

            // version 0 should be stored as a separate entity
            var version0 = versionInfo.Versions[0] as PropertyEntity<string>;
            Assert.IsTrue(version0 != ent);
            Assert.IsTrue(version0.Id != ent.Id);
            Assert.IsTrue(version0.Value == ent.Value);

            var oldEntityId = ent.Id;

            store.Deregister(ent);

            Assert.IsTrue(ent.Id == -1);

            // version info and version0 should still be there
            // a new mutation should be added
            // as well as a version1 for deregisting
            Assert.IsTrue(registry.EntitiesInScope == 5);
            Assert.IsTrue(registry.SlotCount == 6);

            Assert.IsTrue(store.Mutations.Count() == 2);

            var commitMutation = store.Mutations.ElementAt(0) as CommitEntity;

            Assert.IsTrue(commitMutation != null);
            Assert.IsTrue(commitMutation.MutatedEntityId == oldEntityId);
            Assert.IsTrue(commitMutation.MutatedEntity == ent);

            var deregisterMutation = store.Mutations.ElementAt(1) as DeregisterEntity;

            Assert.IsTrue(deregisterMutation != null);
            Assert.IsTrue(deregisterMutation.MutatedEntityId == oldEntityId);
            Assert.IsTrue(deregisterMutation.MutatedEntity == ent);

            Assert.IsTrue(versionInfo.Versions.Count == 2);
        }

        [TestMethod]
        public void RegisterUndoTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);

            var ent = store.Commit(new PropertyEntity<string>()
            {
                Value = "foo"
            });

            ent.Value = "bar";

            Assert.IsTrue(ent.Id >= 0);

            Assert.IsTrue(store.CurrentMutation == 1);

            var entId = ent.Id;

            store.Undo();

            Assert.IsTrue(ent.Value == "foo");

            Assert.IsTrue(store.CurrentMutation == 0);

            // undo doesn't remove the entities as we need to keep the versioninfo around
            Assert.IsTrue(registry[entId] != null);
        }

        [TestMethod]
        public void RegisterCommitUndoTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);

            var ent = store.Commit(new PropertyEntity<string>()
            {
                Value = "foo"
            });

            ent.Value = "bar";

            store.Commit(ent);

            Assert.IsTrue(store.CurrentMutation == 2);

            store.Undo();

            Assert.IsTrue(ent.Value == "bar");
            Assert.IsTrue(store.CurrentMutation == 1);

            store.Undo();

            Assert.IsTrue(ent.Value == "foo");
            Assert.IsTrue(store.CurrentMutation == 0);
        }

        [TestMethod]
        public void RegisterCommitUndoRedoTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);

            var ent = store.Commit(new PropertyEntity<string>()
            {
                Value = "foo"
            });

            ent.Value = "bar";
            store.Commit(ent);

            ent.Value = "qez";
            store.Commit(ent);

            // go to the beginning, head is assumed to be ahead (different)
            // from the last commit so the first undo will result in the 
            // last commit
            store.Undo();
            Assert.IsTrue(ent.Value == "qez");

            store.Undo();
            Assert.IsTrue(ent.Value == "bar");

            store.Undo();
            Assert.IsTrue(ent.Value == "foo");

            Assert.IsFalse(store.Undo());

            // go to the head
            store.Redo();
            Assert.IsTrue(ent.Value == "foo");

            store.Redo();
            Assert.IsTrue(ent.Value == "foo");

            store.Redo();
            Assert.IsTrue(ent.Value == "bar");

            store.Redo();
            Assert.IsTrue(ent.Value == "qez");

            Assert.IsFalse(store.Redo());

            // go half way back (head: nop, M = M2)
            store.Undo();
            Assert.IsTrue(ent.Value == "qez");

            // M2: ent = bar, M = M1
            store.Undo();
            Assert.IsTrue(ent.Value == "bar");

            // go to the end again
            store.Redo();
            Assert.IsTrue(ent.Value == "bar");

            store.Redo();
            Assert.IsTrue(ent.Value == "qez");


            Assert.IsFalse(store.Redo());
        }

        // check if the mutations cut off after the max history count
        // has been reached
        [TestMethod]
        public void CutOffTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry)
            {
                MaxMutations = 2
            };

            var ent = store.Commit(new PropertyEntity<string>("foo"));
            var values = new[] { "bar", "qez", "xim" };

            foreach (var str in values)
            {
                ent.Value = str;
                store.Commit(ent);
            }

            Assert.IsTrue(store.Mutations.Count() == 2);
            // head and two versions
            Assert.IsTrue(registry.Where(kvp => kvp.Value is PropertyEntity<string>).Count() == 3);
            Assert.IsTrue(registry.Where(kvp => kvp.Value is VersionInfo).Count() == 1);
        }

        // check if the mutations cut off after the max history count
        // has been reached and check if undo redo still works as expected
        [TestMethod]
        public void CutOffAndUndoRedoTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry)
            {
                MaxMutations = 2
            };

            var ent = store.Commit(new PropertyEntity<string>("foo"));
            var values = new[] { "bar", "qez", "xim" };

            foreach (var str in values)
            {
                ent.Value = str;
                store.Commit(ent);
            }

            while (store.Undo()) { }

            Assert.IsTrue(ent.Value == "qez");

            while (store.Redo()) { }

            Assert.IsTrue(ent.Value == "xim");
        }

        // check if the mutations are overwritten when a commit takes 
        // place when the head is not at the tail
        [TestMethod]
        public void UndoAndCommitTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent = store.Commit(new PropertyEntity<string>("foo"));
            var values = new[] { "bar", "qez", "xim" };

            foreach (var str in values)
            {
                ent.Value = str;
                store.Commit(ent);
            }

            store.Undo(3);

            Assert.IsTrue(ent.Value == "bar");

            ent.Value = "tun";
            store.Commit(ent);

            // versions should be foo and tun
            Assert.IsTrue(store.MutationCount == 2);

            // ent, 2 versions, versioninfo, 2 mutations
            Assert.IsTrue(registry.EntitiesInScope == 6);

            Assert.IsFalse(store.Redo());

            store.Undo();
            store.Undo();
            Assert.IsTrue(ent.Value == "foo");

            // store is now at tail
            store.Redo();
            Assert.IsTrue(ent.Value == "foo");

            store.Redo();
            Assert.IsTrue(ent.Value == "tun");

            Assert.IsFalse(store.Redo());
        }

        // check if ALL the mutations are overwritten when a commit takes 
        // place when the head is at the tail
        [TestMethod]
        public void UndoAllAndCommitTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent = store.Commit(new PropertyEntity<string>("foo"));
            var values = new[] { "bar", "qez", "xim" };

            foreach (var str in values)
            {
                ent.Value = str;
                store.Commit(ent);
            }

            store.ToTail();

            Assert.IsFalse(registry.Contains(ent));
            Assert.IsTrue(ent.Value == "foo");

            ent.Value = "tun";
            store.Commit(ent);

            Assert.IsTrue(ent.Id >= 0);

            // all versions should be gone now except tun
            Assert.IsTrue(store.MutationCount == 1);

            // ent, 1 versions, versioninfo, 1 mutations
            Assert.IsTrue(registry.EntitiesInScope == 4);
            Assert.IsTrue(ent.Value == "tun");

            var versionInfo = (VersionInfo)registry.First(e => e.Value is VersionInfo).Value;

            Assert.IsTrue(versionInfo.HeadId == ent.Id);
            Assert.IsTrue(versionInfo.Versions.Count == 1);
            var version = versionInfo.Versions[0] as PropertyEntity<string>;

            Assert.IsTrue(version.Value == "tun");

            Assert.IsFalse(store.Redo());
            Assert.IsTrue(store.Undo());

            // should still be tun
            Assert.IsTrue(ent.Value == "tun");

            Assert.IsFalse(store.Undo());
            Assert.IsTrue(store.Redo());
        }

        // Do the same as UndoAllAndCommitTest but with a new entity this time
        [TestMethod]
        public void UndoAllAndRegisterTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent1 = store.Commit(new PropertyEntity<string>("foo"));

            var values = new[] { "bar", "qez", "xim" };

            foreach (var str in values)
            {
                ent1.Value = str;
                store.Commit(ent1);
            }

            store.Undo(-1);

            Assert.IsTrue(ent1.Value == "foo");

            var ent2 = store.Commit(new PropertyEntity<int>(42));

            // all versions should be gone now except tun
            Assert.IsTrue(store.MutationCount == 1);

            // 1 x ent, 1 versions, 1 x versioninfo, 1 mutations
            Assert.IsTrue(registry.EntitiesInScope == 4);
            Assert.IsTrue(ent1.Value == "foo");
            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));

            var versionInfo = (VersionInfo)registry.First(e => e.Value is VersionInfo vi && vi.HeadId == ent2.Id).Value;

            Assert.IsTrue(versionInfo.HeadId == ent2.Id);
            Assert.IsTrue(versionInfo.Versions.Count == 1);
            var version = versionInfo.Versions[0] as PropertyEntity<int>;

            Assert.IsTrue(version.Value == 42);

            Assert.IsFalse(store.Redo());
            Assert.IsTrue(store.Undo());

            // should still be tun
            Assert.IsTrue(ent1.Value == "foo");
            Assert.IsTrue(ent2.Value == 42);

            Assert.IsFalse(store.Undo());
            Assert.IsTrue(store.Redo());
        }

        // Do the same as UndoAllAndCommitTest but with a deregister
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void UndoAllAndDeregisterTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent = store.Commit(new PropertyEntity<string>("foo"));

            var values = new[] { "bar", "qez", "xim" };

            foreach (var str in values)
            {
                ent.Value = str;
                store.Commit(ent);
            }

            store.Undo(-1);

            Assert.IsFalse(registry.Contains(ent));
            Assert.IsTrue(ent.Value == "foo");

            // should throw a contract exception, cannot deregister
            // an entity which is not registered
            store.Deregister(ent);
        }

        // Do the same as UndoAllAndCommitTest but with a deregister
        [TestMethod]
        public void UndoSomeAndDeregisterTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent = store.Commit(new PropertyEntity<string>("foo"));

            var values = new[] { "bar", "qez", "xim" };

            foreach (var str in values)
            {
                ent.Value = str;
                store.Commit(ent);
            }

            store.Undo(2);

            Assert.IsTrue(ent.Value == "qez");

            var oldEntId = ent.Id;
            store.Deregister(ent);

            // foo, bar, dereg
            Assert.IsTrue(store.MutationCount == 3);

            Assert.IsTrue(registry.EntitiesInScope == 7);
            Assert.IsTrue(registry.SlotCount == 8);
            Assert.IsFalse(registry.Contains(ent));

            var versionInfo = (VersionInfo)registry.FirstOrDefault(e => e.Value is VersionInfo vi && vi.HeadId == oldEntId).Value;

            Assert.IsNotNull(versionInfo);

            Assert.IsFalse(store.Redo());
            Assert.IsTrue(store.Undo());

            Assert.IsTrue(registry.Contains(ent));
            Assert.IsTrue(ent.Value == "qez");

            Assert.IsTrue(store.Undo());
            Assert.IsTrue(registry.Contains(ent));
            Assert.IsTrue(ent.Value == "bar");

            Assert.IsTrue(store.Undo());
            Assert.IsTrue(registry.Contains(ent));
            Assert.IsTrue(ent.Value == "foo");

            Assert.IsFalse(store.Undo());
            Assert.IsFalse(registry.Contains(ent));
            Assert.IsTrue(ent.Id == -1);

            // at tail, 1st redo = nop, M => 0
            Assert.IsTrue(store.Redo());
            Assert.IsFalse(registry.Contains(ent));
            Assert.IsTrue(ent.Id == -1);

            Assert.IsTrue(store.Redo());
            Assert.IsTrue(registry.Contains(ent));
            Assert.IsTrue(ent.Id >= 0);
            Assert.IsTrue(ent.Value == "foo");

            Assert.IsTrue(store.Redo());
            Assert.IsTrue(registry.Contains(ent));
            Assert.IsTrue(ent.Value == "bar");

            Assert.IsTrue(store.Redo());
            Assert.IsFalse(registry.Contains(ent));
            Assert.IsTrue(ent.Value == "qez");
            Assert.IsTrue(ent.Id == -1);

            Assert.IsFalse(store.Redo());
        }

        [TestMethod]
        public void RevertTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent = store.Commit(new PropertyEntity<string>("foo"));
            var currentMutation = store.CurrentMutation;

            ent.Value = "bar";
            store.Revert(ent);

            // entity should be reverted to its last know state
            Assert.IsTrue(ent.Value == "foo");
            // should not count/affect the number of mutations
            Assert.IsTrue(currentMutation == store.CurrentMutation);
        }

        // simple back and forth with three entities
        [TestMethod]
        public void MutlipleEntityTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent1 = store.Commit(new PropertyEntity<string>("foo"));
            var ent2 = store.Commit(new PropertyEntity<string>("bar"));
            var ent3 = store.Commit(new PropertyEntity<string>("qez"));

            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
            Assert.IsTrue(registry.Contains(ent3));

            store.ToTail();

            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsFalse(registry.Contains(ent2));
            Assert.IsFalse(registry.Contains(ent3));

            store.ToHead();

            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
            Assert.IsTrue(registry.Contains(ent3));

        }

        // simple back and forth with three entities
        [TestMethod]
        public void MutlipleEntityWithOverwriteTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry)
            {
                MaxMutations = -1
            };

            var entities = new[] {
                store.Commit(new PropertyEntity<string>("foo")),
                store.Commit(new PropertyEntity<string>("bar")),
                store.Commit(new PropertyEntity<string>("qez"))
            };

            // keep the ids so we can track the version info even if 
            // the entities go out of scope
            var entityIds = new[]
            {
                entities[0].Id,
                entities[1].Id,
                entities[2].Id,
            };

            // verify initial version state
            var entityVersion = store.GetVersionInfo(entities[0]);

            Assert.IsTrue(entityVersion.Versions.Count == 1);
            Assert.IsTrue(entityVersion.CurrentVersion == 1);

            entityVersion = store.GetVersionInfo(entities[1]);
            Assert.IsTrue(entityVersion.Versions.Count == 1);
            Assert.IsTrue(entityVersion.CurrentVersion == 1);

            entityVersion = store.GetVersionInfo(entities[2]);
            Assert.IsTrue(entityVersion.Versions.Count == 1);
            Assert.IsTrue(entityVersion.CurrentVersion == 1);


            // modify the entities and commit them, creating
            // new mutations in the store
            for (var i = 0; i < 3; i++)
            {
                Array.ForEach(entities, e =>
                {
                    e.Value += i;
                    store.Commit(e);
                });
            }

            // move back to the point where ent[1] = bar01
            while (entities[1].Value != "bar01")
            {
                store.Undo();
            }

            // verify the store state
            // 3 entities + 3 versionInfo + 4x3 versions + 12 mutations
            Assert.IsTrue(store.MutationCount == 12);
            Assert.IsTrue(registry.EntitiesInScope == 30);

            // verify the version state
            entityVersion = store.GetVersionInfo(entities[0]);

            Assert.IsTrue(entityVersion.Versions.Count == 4);
            Assert.IsTrue(entityVersion.CurrentVersion == 3);

            entityVersion = store.GetVersionInfo(entities[1]);
            Assert.IsTrue(entityVersion.Versions.Count == 4);
            Assert.IsTrue(entityVersion.CurrentVersion == 2);

            entityVersion = store.GetVersionInfo(entities[2]);
            Assert.IsTrue(entityVersion.Versions.Count == 4);
            Assert.IsTrue(entityVersion.CurrentVersion == 2);

            entities[1].Value = "clearbar";

            // this will cut off several mutations
            store.Commit(entities[1]);

            // verify the store state
            // 3 entities + 3 versionInfo + 4x2 versions + 8 mutations
            Assert.IsTrue(store.MutationCount == 8);
            Assert.IsTrue(registry.EntitiesInScope == 22);
            Assert.IsTrue(store.CurrentMutation == 8);

            // verify the version state (all versions should be at the head)
            entityVersion = store.GetVersionInfo(entities[0]);
            Assert.IsTrue(entityVersion.Versions.Count == 3);
            Assert.IsTrue(entityVersion.CurrentVersion == 3);

            entityVersion = store.GetVersionInfo(entities[1]);
            Assert.IsTrue(entityVersion.Versions.Count == 3);
            Assert.IsTrue(entityVersion.CurrentVersion == 3);

            entityVersion = store.GetVersionInfo(entities[2]);
            Assert.IsTrue(entityVersion.Versions.Count == 2);
            Assert.IsTrue(entityVersion.CurrentVersion == 2);

            // go back to the tail
            store.ToTail();

            Array.ForEach(entities, e => Assert.IsFalse(registry.Contains(e)));

            // verify the version state (all versions should be at the tail)
            entityVersion = store.GetVersionInfo(entityIds[0]);
            Assert.IsTrue(entityVersion.Versions.Count == 3);
            Assert.IsTrue(entityVersion.CurrentVersion == -1);

            entityVersion = store.GetVersionInfo(entityIds[1]);
            Assert.IsTrue(entityVersion.Versions.Count == 3);
            Assert.IsTrue(entityVersion.CurrentVersion == -1);

            entityVersion = store.GetVersionInfo(entityIds[2]);
            Assert.IsTrue(entityVersion.Versions.Count == 2);
            Assert.IsTrue(entityVersion.CurrentVersion == -1);

            // go back to the head
            store.ToHead();

            Array.ForEach(entities, e => Assert.IsTrue(registry.Contains(e)));

            Assert.IsTrue(entities[0].Value == "foo01");
            Assert.IsTrue(entities[1].Value == "clearbar");
            Assert.IsTrue(entities[2].Value == "qez0");

            // verify the version state
            entityVersion = store.GetVersionInfo(entityIds[0]);
            Assert.IsTrue(entityVersion.Versions.Count == 3);
            Assert.IsTrue(entityVersion.CurrentVersion == 3);

            entityVersion = store.GetVersionInfo(entityIds[1]);
            Assert.IsTrue(entityVersion.Versions.Count == 3);
            Assert.IsTrue(entityVersion.CurrentVersion == 3);

            entityVersion = store.GetVersionInfo(entityIds[2]);
            Assert.IsTrue(entityVersion.Versions.Count == 2);
            Assert.IsTrue(entityVersion.CurrentVersion == 2);
        }

        [TestMethod]
        public void RemoveOldestCommitMutationTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent1 = new PropertyEntity<string>("foo");
            var ent2 = new PropertyEntity<string>("bar");
            store.Commit(ent1);
            store.Commit(ent2);

            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
            Assert.IsTrue(store.MutationCount == 2);
            Assert.IsTrue(store.CurrentMutation == 2);
            Assert.IsTrue(registry.EntitiesInScope == 8);
            Assert.IsTrue(registry.SlotCount == 8);

            store.DeleteMutation(0);

            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
            Assert.IsTrue(store.MutationCount == 1);
            Assert.IsTrue(store.CurrentMutation == 1);
            Assert.IsTrue(registry.EntitiesInScope == 4);
            Assert.IsTrue(registry.SlotCount == 4);

            store.DeleteMutation(0);

            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsFalse(registry.Contains(ent2));
            Assert.IsTrue(store.MutationCount == 0);
            Assert.IsTrue(store.CurrentMutation == 0);
            Assert.IsTrue(registry.EntitiesInScope == 0);
            Assert.IsTrue(registry.SlotCount == 0);
        }

        [TestMethod]
        public void RemoveOldestDeregisterMutationTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent1 = new PropertyEntity<string>("foo");
            var ent2 = new PropertyEntity<string>("bar");
            store.Commit(ent1);
            store.Deregister(ent1);
            store.Commit(ent2);

            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
            Assert.IsTrue(store.MutationCount == 3);
            Assert.IsTrue(store.CurrentMutation == 3);
            Assert.IsTrue(registry.EntitiesInScope == 9);
            Assert.IsTrue(registry.SlotCount == 10);

            store.DeleteMutation(0);

            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
            Assert.IsTrue(store.MutationCount == 2);
            Assert.IsTrue(store.CurrentMutation == 2);
            Assert.IsTrue(registry.EntitiesInScope == 7);
            Assert.IsTrue(registry.SlotCount == 8);

            store.DeleteMutation(0);

            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
            Assert.IsTrue(store.MutationCount == 1);
            Assert.IsTrue(store.CurrentMutation == 1);
            Assert.IsTrue(registry.EntitiesInScope == 4);
            Assert.IsTrue(registry.SlotCount == 4);

            store.DeleteMutation(0);

            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsFalse(registry.Contains(ent2));
            Assert.IsTrue(store.MutationCount == 0);
            Assert.IsTrue(store.CurrentMutation == 0);
            Assert.IsTrue(registry.EntitiesInScope == 0);
            Assert.IsTrue(registry.SlotCount == 0);
        }

        /// <summary>
        /// Remove the last commit in a row of three different entities, see if everything is peachy.
        /// </summary>
        [TestMethod]
        public void RemoveLastCommitTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent1 = store.Commit(new PropertyEntity<string>("foo"));
            var ent2 = store.Commit(new PropertyEntity<string>("bar"));
            var ent3 = store.Commit(new PropertyEntity<string>("qaz"));

            // verify pre-state
            Assert.IsTrue(registry.Count(kvp => kvp.Value is VersionInfo) == 3);
            Assert.IsTrue(registry.EntitiesInScope == 12);
            Assert.IsTrue(store.MutationCount == 3);
            Assert.IsTrue(store.CurrentMutation == 3);

            store.DeleteMutation(2);

            // test outcome
            Assert.IsTrue(store.CurrentMutation == 2);
            Assert.IsTrue(store.MutationCount == 2);

            Assert.IsTrue(registry.EntitiesInScope == 8);
            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
            Assert.IsFalse(registry.Contains(ent3));

            Assert.IsTrue((store.GetMutation(0) as CommitEntity).MutatedEntityId == ent1.Id);
            Assert.IsTrue((store.GetMutation(1) as CommitEntity).MutatedEntityId == ent2.Id);

            Assert.IsTrue(registry.Count(e => e.Value is VersionInfo) == 2);
        }

        /// <summary>
        /// Remove the middle commit in a row of three different entities, see if everything is peachy.
        /// </summary>
        [TestMethod]
        public void RemoveMidCommitTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent1 = store.Commit(new PropertyEntity<string>("foo"));
            var ent2 = store.Commit(new PropertyEntity<string>("bar"));
            var ent3 = store.Commit(new PropertyEntity<string>("qaz"));

            // verify pre-state
            Assert.IsTrue(registry.Count(e => e.Value is VersionInfo) == 3);
            Assert.IsTrue(registry.EntitiesInScope == 12);
            Assert.IsTrue(store.CurrentMutation == 3);

            store.DeleteMutation(1);

            // test outcome
            Assert.IsTrue(store.CurrentMutation == 2);
            Assert.IsTrue(store.MutationCount == 2);

            Assert.IsTrue(registry.EntitiesInScope == 8);
            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsFalse(registry.Contains(ent2));
            Assert.IsTrue(registry.Contains(ent3));

            Assert.IsTrue((store.GetMutation(0) as CommitEntity).MutatedEntityId == ent1.Id);
            Assert.IsTrue((store.GetMutation(1) as CommitEntity).MutatedEntityId == ent3.Id);

            Assert.IsTrue(registry.Count(e => e.Value is VersionInfo) == 2);
        }

        /// <summary>
        /// Remove the first commit in a row of one and the same entity, see if everything is peachy.
        /// </summary>
        [TestMethod]
        public void RemoveFirstCommitSingleEntityTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent1 = store.Commit(new PropertyEntity<string>("foo"));
            store.Commit(ent1.With("bar"));
            store.Commit(ent1.With("qaz"));

            // verify pre-state
            var version = store.GetVersionInfo(ent1);
            Assert.IsTrue(version.Versions.Count == 3);
            Assert.IsTrue(version.CurrentVersion == 3);
            Assert.IsTrue(registry.EntitiesInScope == 8);
            Assert.IsTrue(store.CurrentMutation == 3);
            Assert.IsTrue(store.MutationCount == 3);

            store.DeleteMutation(0);

            // test outcome
            Assert.IsTrue(store.CurrentMutation == 2);
            Assert.IsTrue(store.MutationCount == 2);

            Assert.IsTrue(registry.EntitiesInScope == 6);
            Assert.IsTrue(registry.Contains(ent1));

            Assert.IsTrue((store.GetMutation(0) as CommitEntity).MutatedEntityId == ent1.Id);
            Assert.IsTrue((store.GetMutation(1) as CommitEntity).MutatedEntityId == ent1.Id);

            Assert.IsTrue(registry.Count(e => e.Value is VersionInfo) == 1);
            Assert.IsTrue(version.Versions.Count == 2);
            Assert.IsTrue(version.CurrentVersion == 2);

            store.ToTail();

            Assert.IsTrue(ent1.Value == "bar");

            store.ToHead();

            Assert.IsTrue(ent1.Value == "qaz");
        }

        /// <summary>
        /// Remove the first commit in a row of one and the same entity, see if everything is peachy.
        /// </summary>
        [TestMethod]
        public void RemoveMiddleCommitSingleEntityTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent = store.Commit(new PropertyEntity<string>("foo"));
            store.Commit(ent.With("bar"));
            store.Commit(ent.With("qaz"));

            // verify pre-state
            var version = store.GetVersionInfo(ent);
            Assert.IsTrue(version.Versions.Count == 3);
            Assert.IsTrue(version.CurrentVersion == 3);
            Assert.IsTrue(registry.EntitiesInScope == 8);
            Assert.IsTrue(store.CurrentMutation == 3);
            Assert.IsTrue(store.MutationCount == 3);

            store.DeleteMutation(1);

            // test outcome
            Assert.IsTrue(store.CurrentMutation == 2);
            Assert.IsTrue(store.MutationCount == 2);

            Assert.IsTrue(registry.EntitiesInScope == 6);
            Assert.IsTrue(registry.Contains(ent));

            Assert.IsTrue((store.GetMutation(0) as CommitEntity).MutatedEntityId == ent.Id);
            Assert.IsTrue((store.GetMutation(1) as CommitEntity).MutatedEntityId == ent.Id);

            Assert.IsTrue(registry.Count(e => e.Value is VersionInfo) == 1);
            Assert.IsTrue(version.Versions.Count == 2);
            Assert.IsTrue(version.CurrentVersion == 2);

            store.ToTail();

            Assert.IsTrue(ent.Value == "foo");

            store.ToHead();

            Assert.IsTrue(ent.Value == "qaz");
        }

        /// <summary>
        /// Remove the first commit in a row of one and the same entity, see if everything is peachy.
        /// </summary>
        [TestMethod]
        public void RemoveLastCommitSingleEntityTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent = store.Commit(new PropertyEntity<string>("foo"));
            store.Commit(ent.With("bar"));
            store.Commit(ent.With("qaz"));

            // verify pre-state
            var version = store.GetVersionInfo(ent);
            Assert.IsTrue(version.Versions.Count == 3);
            Assert.IsTrue(version.CurrentVersion == 3);
            Assert.IsTrue(registry.EntitiesInScope == 8);
            Assert.IsTrue(store.CurrentMutation == 3);
            Assert.IsTrue(store.MutationCount == 3);

            store.DeleteMutation(2);

            // test outcome
            Assert.IsTrue(store.CurrentMutation == 2);
            Assert.IsTrue(store.MutationCount == 2);

            Assert.IsTrue(registry.EntitiesInScope == 6);
            Assert.IsTrue(registry.Contains(ent));

            Assert.IsTrue((store.GetMutation(0) as CommitEntity).MutatedEntityId == ent.Id);
            Assert.IsTrue((store.GetMutation(1) as CommitEntity).MutatedEntityId == ent.Id);

            Assert.IsTrue(registry.Count(kvp => kvp.Value is VersionInfo) == 1);
            Assert.IsTrue(version.Versions.Count == 2);
            Assert.IsTrue(version.CurrentVersion == 2);

            store.ToTail();

            Assert.IsTrue(ent.Value == "foo");

            store.ToHead();

            Assert.IsTrue(ent.Value == "bar");
        }


        /// <summary>
        /// Remove the first deregister, see if everything is peachy.
        /// </summary>
        [TestMethod]
        public void RemoveDeregisterFirstTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent1 = store.Commit(new PropertyEntity<string>("foo"));
            store.Deregister(ent1);
            var ent2 = store.Commit(new PropertyEntity<string>("bar"));
            var ent3 = store.Commit(new PropertyEntity<string>("qaz"));

            // verify pre-state
            Assert.IsTrue(registry.EntitiesInScope == 13);
            Assert.IsTrue(store.CurrentMutation == 4);
            Assert.IsTrue(store.MutationCount == 4);
            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
            Assert.IsTrue(registry.Contains(ent3));

            // remove the first commit
            store.DeleteMutation(0);

            // remove the deregister 
            store.DeleteMutation(0);

            // test outcome
            Assert.IsTrue(store.CurrentMutation == 2);
            Assert.IsTrue(store.MutationCount == 2);

            Assert.IsTrue(registry.EntitiesInScope == 8);
            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
            Assert.IsTrue(registry.Contains(ent3));
        }

        /// <summary>
        /// Remove the middle deregister, see if everything is peachy. This
        /// should bring back the entity which was deleted
        /// </summary>
        [TestMethod]
        public void RemoveDeregisterInMiddlePositionTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent1 = store.Commit(new PropertyEntity<string>("foo"));
            store.Commit(ent1.With("bar"));
            store.Deregister(ent1);
            var ent2 = store.Commit(new PropertyEntity<string>("qaz"));

            // verify pre-state
            Assert.IsTrue(registry.EntitiesInScope == 11);
            Assert.IsTrue(store.CurrentMutation == 4);
            Assert.IsTrue(store.MutationCount == 4);
            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));

            // remove the deregister 
            store.DeleteMutation(2);

            // test outcome
            Assert.IsTrue(store.CurrentMutation == 3);
            Assert.IsTrue(store.MutationCount == 3);

            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
            Assert.IsTrue(registry.EntitiesInScope == 10);
            Assert.IsTrue(ent1.Value == "bar");
            Assert.IsTrue(ent1.Id >= 0);
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
            var store = new EntityHistory(registry);
            var ent1 = store.Commit(new PropertyEntity<string>("foo"));
            store.Commit(ent1.With("bar"));
            store.Deregister(ent1);
            var ent2 = store.Commit(new PropertyEntity<string>("qaz"));

            // ent1 initial commit "foo" = 4
            // commit "bar" = +2 (6)
            // deregister = +2, -1 (7)
            // ent2 = +=4 (11)
            Assert.IsTrue(registry.EntitiesInScope == 11);

            // verify pre-state
            store.ToTail();

            // should have deregistered ent2 so entities in scope should be 11            
            Assert.IsTrue(registry.EntitiesInScope == 10);
            Assert.IsTrue(store.CurrentMutation == -1);
            Assert.IsTrue(store.MutationCount == 4);
            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsFalse(registry.Contains(ent2));

            // remove the deregister 
            store.DeleteMutation(2);

            // test outcome
            Assert.IsTrue(store.CurrentMutation == -1);
            Assert.IsTrue(store.MutationCount == 3);

            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsFalse(registry.Contains(ent2));
            Assert.IsTrue(registry.EntitiesInScope == 8);
            Assert.IsTrue(ent1.Id < 0);

            store.ToHead();

            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
            Assert.IsTrue(ent1.Value == "bar");
        }

        /// <summary>
        /// Remove the middle deregister after going undo before the deregister, see if everything is peachy. This
        /// should NOT bring back the entity which was deleted because the entity
        /// is not in scope yet
        /// </summary>
        [TestMethod]
        public void RemoveDeregisterInMiddlePositionAfterUndoBeforeDeregisterTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent1 = store.Commit(new PropertyEntity<string>("foo"));
            store.Commit(ent1.With("bar"));
            store.Deregister(ent1);
            var ent2 = store.Commit(new PropertyEntity<string>("qaz"));

            // verify pre-state
            store.Undo();
            store.Undo();
            store.Undo();

            Assert.IsTrue(registry.EntitiesInScope == 11);
            Assert.IsTrue(store.CurrentMutation == 1);
            Assert.IsTrue(store.MutationCount == 4);
            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsFalse(registry.Contains(ent2));

            // remove the deregister 
            store.DeleteMutation(2);

            // test outcome
            Assert.IsTrue(store.CurrentMutation == 1);
            Assert.IsTrue(store.MutationCount == 3);

            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsFalse(registry.Contains(ent2));
            Assert.IsTrue(registry.EntitiesInScope == 9);

            store.ToHead();

            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
            Assert.IsTrue(ent1.Value == "bar");
        }

        /// <summary>
        /// Remove the last deregister, see if everything is peachy. This
        /// should bring back the entity which was deleted
        /// </summary>
        [TestMethod]
        public void DeleteDeregisterInLastPositionTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent1 = store.Commit(new PropertyEntity<string>("foo"));
            store.Commit(ent1.With("bar"));
            var ent2 = store.Commit(new PropertyEntity<string>("qaz"));
            store.Deregister(ent1);

            // verify pre-state
            Assert.IsTrue(registry.EntitiesInScope == 11);
            Assert.IsTrue(store.CurrentMutation == 4);
            Assert.IsTrue(store.MutationCount == 4);
            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));

            // remove the deregister 
            store.DeleteMutation(3);

            // test outcome
            Assert.IsTrue(store.CurrentMutation == 3);
            Assert.IsTrue(store.MutationCount == 3);

            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
            Assert.IsTrue(registry.EntitiesInScope == 10);
            Assert.IsTrue(ent1.Value == "bar");
            Assert.IsTrue(ent1.Id >= 0);
        }

        // xxx to do test delete begin group
        // xxx to do test delete end group (should throw an exception)

        /// <summary>
        /// Delete a group at the beginning of a set of changes
        /// </summary>
        [TestMethod]
        public void DeleteBeginGroupInFirstPositionTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            store.BeginMutationGroup();
            var ent1 = store.Commit(new PropertyEntity<string>("foo"));
            store.Commit(ent1.With("bar"));
            store.EndMutationGroup();
            var ent2 = store.Commit(new PropertyEntity<string>("qaz"));

            // verify pre-state
            Assert.IsTrue(registry.EntitiesInScope == 12);
            Assert.IsTrue(store.CurrentMutation == 5);
            Assert.IsTrue(store.MutationCount == 5);
            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));

            // remove the begin
            store.DeleteMutation(0);

            Assert.IsTrue(registry.EntitiesInScope == 4);
            Assert.IsTrue(registry.SlotCount == 4);
            Assert.IsTrue(store.CurrentMutation == 1);
            Assert.IsTrue(store.MutationCount == 1);
            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
        }

        /// <summary>
        /// Delete a group at the beginning of a set of changes
        /// </summary>
        [TestMethod]
        public void DeleteBeginGroupInMiddlePositionTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent1 = store.Commit(new PropertyEntity<string>("foo"));
            store.BeginMutationGroup();
            store.Commit(new PropertyEntity<string>("bar"));
            store.Commit(ent1.With("qaz"));
            store.EndMutationGroup();
            var ent2 = store.Commit(new PropertyEntity<string>("thud"));

            // verify pre-state
            Assert.IsTrue(registry.EntitiesInScope == 16);
            Assert.IsTrue(store.CurrentMutation == 6);
            Assert.IsTrue(store.MutationCount == 6);
            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));

            // remove the group in the middle
            store.DeleteMutation(1);

            Assert.IsTrue(registry.EntitiesInScope == 8);
            Assert.IsTrue(registry.SlotCount == 8);
            Assert.IsTrue(store.CurrentMutation == 2);
            Assert.IsTrue(store.MutationCount == 2);
            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
            Assert.IsTrue(store.GetVersionInfo(ent1).CurrentVersion == 1);
            Assert.IsTrue(store.GetVersionInfo(ent2).CurrentVersion == 1);

            Assert.IsTrue(ent1.Value == "qaz");
            Assert.IsTrue(ent2.Value == "thud");
        }

        /// <summary>
        /// Delete a group at the beginning of a set of changes
        /// </summary>
        [TestMethod]
        public void DeleteEndGroupInMiddlePositionTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent1 = store.Commit(new PropertyEntity<string>("foo"));
            store.BeginMutationGroup();
            store.Commit(ent1.With("qaz"));
            var ent2 = store.Commit(new PropertyEntity<string>("thud"));
            store.EndMutationGroup();


            // verify pre-state
            Assert.IsTrue(registry.EntitiesInScope == 12);
            Assert.IsTrue(store.CurrentMutation == 5);
            Assert.IsTrue(store.MutationCount == 5);
            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));

            // remove the group at the end
            store.DeleteMutation(1);

            Assert.IsTrue(registry.EntitiesInScope == 4);
            Assert.IsTrue(registry.SlotCount == 4);
            Assert.IsTrue(store.CurrentMutation == 1);
            Assert.IsTrue(store.MutationCount == 1);
            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsFalse(registry.Contains(ent2));
        }

        /// <summary>
        /// Deleting an end group should throw an exception
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DeleteEndGroupShouldTrowException()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);
            var ent1 = store.Commit(new PropertyEntity<string>("foo"));
            store.BeginMutationGroup();
            store.Commit(ent1.With("qaz"));
            store.EndMutationGroup();

            // remove the group at the end
            store.DeleteMutation(3);
        }
    }
}
