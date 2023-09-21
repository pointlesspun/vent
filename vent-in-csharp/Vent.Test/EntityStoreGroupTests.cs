/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

namespace Vent.Test
{
    [TestClass]
    public class EntityStoreGroupTests
    {
        [TestMethod]
        public void BeginGroupAndUndoTests()
        {
            var store = new EntityHistory(new EntityRegistry());

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
            Assert.IsTrue(store.EntitiesInScope == 10);

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
            var store = new EntityHistory(new EntityRegistry());

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
            var store = new EntityHistory(new EntityRegistry());

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
            var store = new EntityHistory(new EntityRegistry())
            {
                MaxMutations = 4
            };

            store.BeginMutationGroup();
            var ent1 = store.Commit(new PropertyEntity<string>("foo"));
            var ent2 = store.Commit(new PropertyEntity<string>("bar"));
            store.EndMutationGroup();

            Assert.IsTrue(store.MutationCount == 4);

            // this should clean up the entire group as this will exceed MaxMutations
            var ent3 = store.Commit(new PropertyEntity<string>("qez"));

            Assert.IsTrue(store.MutationCount == 1);
            Assert.IsTrue(store.CurrentMutation == 1);
            Assert.IsTrue(store.EntitiesInScope == 4);

            Assert.IsFalse(store.Contains(ent1));
            Assert.IsFalse(store.Contains(ent2));
            Assert.IsTrue(store.Contains(ent3));
        }


        /// <summary>
        /// Exceed the max mutation while a group is active,
        /// this should raise a contract exception
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ExceedMaxMutationCountWithGroupTest()
        {
            var store = new EntityHistory(new EntityRegistry())
            {
                MaxMutations = 3
            };

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
            var store = new EntityHistory(new EntityRegistry());

            store.EndMutationGroup();
        }



        /// <summary>
        /// Add a couple of commits, undo all and do a beginGroup/endGroup.
        /// Check if future commits are removed.
        /// </summary>
        [TestMethod]
        public void RemoveFutureMutationAfterBeginGroupTest()
        {
            var store = new EntityHistory(new EntityRegistry());

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
            Assert.IsTrue(store.EntitiesInScope == 16);

            store.ToTail();

            while (ent1.Value != "foo1" && store.Redo()) ;

            Assert.IsTrue(store.CurrentMutation == 3);

            store.BeginMutationGroup();

            Assert.IsTrue(store.MutationCount == 4);
            Assert.IsTrue(store.CurrentMutation == 4);
            Assert.IsTrue(store.EntitiesInScope == 11);
        }



        [TestMethod]
        public void RemoveOldestGroupMutationTest()
        {
            var store = new EntityHistory(new EntityRegistry());

            var ent1 = new PropertyEntity<string>("foo");
            var ent2 = new PropertyEntity<string>("bar");
            var ent3 = new PropertyEntity<string>("qaz");

            store.BeginMutationGroup();

            store.Commit(ent1);
            store.Commit(ent2);

            store.EndMutationGroup();

            store.Commit(ent3);

            Assert.IsTrue(store.Contains(ent1));
            Assert.IsTrue(store.Contains(ent2));
            Assert.IsTrue(store.Contains(ent3));
            Assert.IsTrue(store.MutationCount == 5);
            Assert.IsTrue(store.CurrentMutation == 5);
            Assert.IsTrue(store.EntitiesInScope == 14);
            Assert.IsTrue(store.SlotCount == 14);

            store.DeleteMutation(0);

            Assert.IsFalse(store.Contains(ent1));
            Assert.IsFalse(store.Contains(ent2));
            Assert.IsTrue(store.Contains(ent3));
            Assert.IsTrue(store.MutationCount == 1);
            Assert.IsTrue(store.CurrentMutation == 1);
            Assert.IsTrue(store.EntitiesInScope == 4);
            Assert.IsTrue(store.SlotCount == 4);
        }

        [TestMethod]
        public void RemoveOldestEmptyGroupMutationTest()
        {
            var store = new EntityHistory(new EntityRegistry());

            store.BeginMutationGroup();
            store.EndMutationGroup();

            Assert.IsTrue(store.MutationCount == 2);
            Assert.IsTrue(store.CurrentMutation == 2);
            Assert.IsTrue(store.EntitiesInScope == 2);
            Assert.IsTrue(store.SlotCount == 2);

            store.DeleteMutation(0);

            Assert.IsTrue(store.MutationCount == 0);
            Assert.IsTrue(store.CurrentMutation == 0);
            Assert.IsTrue(store.EntitiesInScope == 0);
            Assert.IsTrue(store.SlotCount == 0);

        }

        [TestMethod]
        public void ShouldBeAbleToRemoveClosedGroupTest()
        {
            var store = new EntityHistory(new EntityRegistry());

            store.BeginMutationGroup();
            store.EndMutationGroup();
            store.BeginMutationGroup();

            Assert.IsFalse(store.IsGroupOpen(0));
            Assert.IsTrue(store.IsGroupOpen(2));

            Assert.IsTrue(store.MutationCount == 3);
            Assert.IsTrue(store.CurrentMutation == 3);
            Assert.IsTrue(store.EntitiesInScope == 3);
            Assert.IsTrue(store.SlotCount == 3);
            Assert.IsTrue(store.OpenGroupCount == 1);

            store.DeleteMutation(0);

            Assert.IsTrue(store.MutationCount == 1);
            Assert.IsTrue(store.CurrentMutation == 1);
            Assert.IsTrue(store.EntitiesInScope == 1);
            Assert.IsTrue(store.SlotCount == 1);
            Assert.IsTrue(store.OpenGroupCount == 1);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ShouldNotBeAbleToRemoveOpenGroupTest()
        {
            var store = new EntityHistory(new EntityRegistry());

            store.BeginMutationGroup();

            // should throw an exception
            store.DeleteMutation(0);
        }
    }
}


