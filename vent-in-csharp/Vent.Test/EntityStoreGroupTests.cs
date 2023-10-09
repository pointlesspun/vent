
/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun
 
using Vent.Entities;
using Vent.History;
using Vent.Registry;

namespace Vent.Test
{
    [TestClass]
    public class EntityStoreGroupTests
    {
        [TestMethod]
        public void BeginGroupAndUndoTests()
        {
            var registry = new EntityRegistry();
            var store = new HistorySystem(registry);

            var ent1 = store.Commit(new PropertyEntity<string>("foo"));

            store.BeginMutationGroup();

            store.Commit(ent1.With("bar"));
            store.Commit(ent1.With("qez"));

            store.EndMutationGroup();

            Assert.IsTrue(ent1.Value == "qez");
            Assert.IsTrue(store.MutationCount == 5);
            Assert.IsTrue(store.CurrentMutation == 5);
            // 1 entity, 1 versioninfo, 3 versions
            // 5 mutations
            // 1 history
            Assert.IsTrue(registry.EntitiesInScope == 11);

            store.Undo();
            Assert.IsTrue(ent1.Value == "bar");
            Assert.IsTrue(store.MutationCount == 5);
            Assert.IsTrue(store.CurrentMutation == 1);

            store.Undo();
            Assert.IsTrue(ent1.Value == "foo");

            Assert.IsTrue(store.CurrentMutation == 0);
        }

        [TestMethod]
        public void BeginGroupUndoAndRedoTests()
        {
            var store = new HistorySystem(new EntityRegistry());

            var ent1 = store.Commit(new PropertyEntity<string>("foo"));

            store.BeginMutationGroup();

            store.Commit(ent1.With("bar"));
            store.Commit(ent1.With("qez"));

            store.EndMutationGroup();

            store.ToTail();

            Assert.IsTrue(ent1.Value == "foo");
            Assert.IsTrue(store.CurrentMutation == -1);

            store.Redo();

            Assert.IsTrue(ent1.Value == "foo");
            Assert.IsTrue(store.CurrentMutation == 0);

            store.Redo();

            Assert.IsTrue(ent1.Value == "foo");
            Assert.IsTrue(store.CurrentMutation == 1);

            store.Redo();

            Assert.IsTrue(ent1.Value == "qez");
            Assert.IsTrue(store.CurrentMutation == 5);
        }

        [TestMethod]
        public void BeginGroupUndoAndRedoInnerGroupTests()
        {
            var store = new HistorySystem(new EntityRegistry());

            var ent1 = store.Commit(new PropertyEntity<string>("foo1"));
            var ent2 = store.Commit(new PropertyEntity<string>("foo2"));

            store.BeginMutationGroup();

            store.Commit(ent1.With("bar1"));
            store.Commit(ent1.With("qez1"));

            store.BeginMutationGroup();

            store.Commit(ent2.With("bar2"));
            store.Commit(ent2.With("qez2"));

            store.EndMutationGroup();

            store.EndMutationGroup();
            store.Undo();

            Assert.IsTrue(ent1.Value == "bar1");
            Assert.IsTrue(ent2.Value == "bar2");

            store.ToTail();

            Assert.IsTrue(ent1.Value == "foo1");
            Assert.IsTrue(ent2.Value == "foo2");

            Assert.IsTrue(store.CurrentMutation == -1);

            store.Redo();

            Assert.IsTrue(ent1.Value == "foo1");
            Assert.IsTrue(ent2.Value == "foo2");
            Assert.IsTrue(store.CurrentMutation == 0);

            // redo commit foo1
            store.Redo();

            Assert.IsTrue(ent1.Value == "foo1");
            Assert.IsTrue(ent2.Value == "foo2");
            Assert.IsTrue(store.CurrentMutation == 1);

            // redo commit foo2
            store.Redo();
            Assert.IsTrue(ent1.Value == "foo1");
            Assert.IsTrue(ent2.Value == "foo2");
            Assert.IsTrue(store.CurrentMutation == 2);

            // redo group
            store.Redo();
            Assert.IsTrue(ent1.Value == "qez1");
            Assert.IsTrue(ent2.Value == "qez2");
            Assert.IsTrue(store.CurrentMutation == store.MutationCount);
        }

        /// <summary>
        /// Start with a group, then exceed the max mutations. Check
        /// if the group gets cleaned up correctly.
        /// </summary>
        [TestMethod]
        public void ExceedMaxMutationCountTest()
        {
            var registry = new EntityRegistry();
            var store = new HistorySystem(registry, registry.Add(new EntityHistory(4)));

            store.BeginMutationGroup();
            var ent1 = store.Commit(new PropertyEntity<string>("foo"));
            var ent2 = store.Commit(new PropertyEntity<string>("bar"));
            store.EndMutationGroup();

            Assert.IsTrue(store.MutationCount == 4);

            // this should clean up the entire group as this will exceed MaxMutations
            var ent3 = store.Commit(new PropertyEntity<string>("qez"));

            Assert.IsTrue(store.MutationCount == 1);
            Assert.IsTrue(store.CurrentMutation == 1);
            Assert.IsTrue(registry.EntitiesInScope == 5);

            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsFalse(registry.Contains(ent2));
            Assert.IsTrue(registry.Contains(ent3));
        }


        /// <summary>
        /// Exceed the max mutation while a group is active,
        /// this should raise a contract exception
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ExceedMaxMutationCountWithGroupTest()
        {
            var store = new HistorySystem(new EntityRegistry(), new EntityHistory(3));

            store.BeginMutationGroup();
            store.Commit(new PropertyEntity<string>("foo"));
            store.Commit(new PropertyEntity<string>("bar"));
            store.Commit(new PropertyEntity<string>("boom"));
        }


        /// <summary>
        /// End called without begin should raise an exception
        /// </summary>

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void EndWithoutBeginShouldThrowExceptionTest()
        {
            var store = new HistorySystem(new EntityRegistry());

            store.EndMutationGroup();
        }



        /// <summary>
        /// Add a couple of commits, undo all and do a beginGroup/endGroup.
        /// Check if future commits are removed.
        /// </summary>
        [TestMethod]
        public void RemoveFutureMutationAfterBeginGroupTest()
        {
            var registry = new EntityRegistry();
            var store = new HistorySystem(registry);

            var ent1 = store.Commit(new PropertyEntity<string>("foo0"));
            var ent2 = store.Commit(new PropertyEntity<string>("bar0"));

            store.Commit(ent1.With("foo1"));
            store.Commit(ent2.With("bar1"));

            store.Commit(ent1.With("foo2"));
            store.Commit(ent2.With("bar2"));

            Assert.IsTrue(ent1.Value == "foo2");
            Assert.IsTrue(ent2.Value == "bar2");
            Assert.IsTrue(store.MutationCount == 6);
            Assert.IsTrue(store.CurrentMutation == 6);
            Assert.IsTrue(registry.EntitiesInScope == 17);

            store.ToTail();

            while (ent1.Value != "foo1" && store.Redo()) ;

            Assert.IsTrue(store.CurrentMutation == 3);

            store.BeginMutationGroup();

            Assert.IsTrue(store.MutationCount == 4);
            Assert.IsTrue(store.CurrentMutation == 4);
            Assert.IsTrue(registry.EntitiesInScope == 12);
        }



        [TestMethod]
        public void RemoveOldestGroupMutationTest()
        {
            var registry = new EntityRegistry();
            var store = new HistorySystem(registry);

            var ent1 = new PropertyEntity<string>("foo");
            var ent2 = new PropertyEntity<string>("bar");
            var ent3 = new PropertyEntity<string>("qaz");

            store.BeginMutationGroup();

            store.Commit(ent1);
            store.Commit(ent2);

            store.EndMutationGroup();

            store.Commit(ent3);

            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
            Assert.IsTrue(registry.Contains(ent3));
            Assert.IsTrue(store.MutationCount == 5);
            Assert.IsTrue(store.CurrentMutation == 5);
            Assert.IsTrue(registry.EntitiesInScope == 15);
            Assert.IsTrue(registry.SlotCount == 15);

            store.DeleteMutation(0);

            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsFalse(registry.Contains(ent2));
            Assert.IsTrue(registry.Contains(ent3));
            Assert.IsTrue(store.MutationCount == 1);
            Assert.IsTrue(store.CurrentMutation == 1);
            Assert.IsTrue(registry.EntitiesInScope == 5);
            Assert.IsTrue(registry.SlotCount == 5);
        }

        [TestMethod]
        public void RemoveOldestEmptyGroupMutationTest()
        {
            var registry = new EntityRegistry();
            var store = new HistorySystem(registry);

            store.BeginMutationGroup();
            store.EndMutationGroup();

            Assert.IsTrue(store.MutationCount == 2);
            Assert.IsTrue(store.CurrentMutation == 2);
            Assert.IsTrue(registry.EntitiesInScope == 3);
            Assert.IsTrue(registry.SlotCount == 3);

            store.DeleteMutation(0);

            Assert.IsTrue(store.MutationCount == 0);
            Assert.IsTrue(store.CurrentMutation == 0);
            Assert.IsTrue(registry.EntitiesInScope == 1);
            Assert.IsTrue(registry.SlotCount == 1);

        }

        [TestMethod]
        public void ShouldBeAbleToRemoveClosedGroupTest()
        {
            var registry = new EntityRegistry();
            var history = new HistorySystem(registry);

            history.BeginMutationGroup();
            history.EndMutationGroup();
            history.BeginMutationGroup();

            Assert.IsFalse(history.IsGroupOpen(0));
            Assert.IsTrue(history.IsGroupOpen(2));

            Assert.IsTrue(history.MutationCount == 3);
            Assert.IsTrue(history.CurrentMutation == 3);
            Assert.IsTrue(registry.EntitiesInScope == 4);
            Assert.IsTrue(registry.SlotCount == 4);
            Assert.IsTrue(history.OpenGroupCount == 1);

            history.DeleteMutation(0);

            Assert.IsTrue(history.MutationCount == 1);
            Assert.IsTrue(history.CurrentMutation == 1);
            Assert.IsTrue(registry.EntitiesInScope == 2);
            Assert.IsTrue(registry.SlotCount == 2);
            Assert.IsTrue(history.OpenGroupCount == 1);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ShouldNotBeAbleToRemoveOpenGroupTest()
        {
            var store = new HistorySystem(new EntityRegistry());

            store.BeginMutationGroup();

            // should throw an exception
            store.DeleteMutation(0);
        }
    }
}


