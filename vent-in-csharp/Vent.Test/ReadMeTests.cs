/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

namespace Vent.Test
{
    /// <summary>
    /// Tests covering the examples in the readme.md
    /// </summary>
    [TestClass]
    public class ReadMeTests
    {
        [TestMethod]
        public void BasicCommitUndoExample()
        {
            // create a new store
            var store = new EntityStore();

            // commit an entity to the store to track information
            var ent = store.Commit(new PropertyEntity<string>("foo"));

            // change the entity (head)
            ent.Value = "bar";

            // commit the current entity state
            store.Commit(ent);

            // change the entity value
            ent.Value = "qez";

            // first undo
            Assert.IsTrue(store.Undo());
            Assert.AreEqual("bar", ent.Value);

            // move to "foo" check if ent is still in the store
            Assert.IsTrue(store.Undo());
            Assert.AreEqual("foo", ent.Value);
            Assert.IsTrue(ent.Id >= 0);
            Assert.IsTrue(store.Contains(ent));

            // cannot move any further, foo has been removed from the store.
            Assert.IsFalse(store.Undo());
            Assert.AreEqual("foo", ent.Value);
            Assert.IsTrue(ent.Id == -1);
            Assert.IsFalse(store.Contains(ent));

            // redo will seem to do nothing
            Assert.IsTrue(store.Redo());
            Assert.AreEqual("foo", ent.Value);
            Assert.IsTrue(ent.Id == -1);
            Assert.IsFalse(store.Contains(ent));

            // until this point ... foo makes it back into the store
            Assert.IsTrue(store.Redo());
            Assert.AreEqual("foo", ent.Value);
            Assert.IsTrue(ent.Id >= 0);
            Assert.IsTrue(store.Contains(ent));

            Assert.IsTrue(store.Redo());
            Assert.AreEqual("bar", ent.Value);
            Assert.IsTrue(ent.Id >= 0);
            Assert.IsTrue(store.Contains(ent));

            // reached the end
            Assert.IsFalse(store.Redo());
        }

        [TestMethod]
        public void CutOffTest()
        {
            // create a new store
            var store = new EntityStore();

            // commit an entity to the store to track information
            var ent = store.Commit(new PropertyEntity<string>("foo"));
            store.Commit(ent.With("bar"));
            store.Commit(ent.With("baz"));
            store.Commit(ent.With("qux"));

            while (ent.Value != "bar" && store.Undo()) ;

            Assert.AreEqual("bar", ent.Value);

            var versions = store.GetVersionInfo(ent)
                                .Versions
                                .Cast<PropertyEntity<string>>()
                                .ToList();

            Assert.IsTrue(versions.Count == 4);
            Assert.IsTrue(versions[0].Value == "foo");
            Assert.IsTrue(versions[1].Value == "bar");
            Assert.IsTrue(versions[2].Value == "baz");
            Assert.IsTrue(versions[3].Value == "qux");


            Assert.IsTrue(store.MutationCount == 4);
            Assert.IsTrue(store.CurrentMutation == 1);

            // cut off will happen here, mutations after and including mutation 1
            // will be removed
            store.Commit(ent.With("thud"));

            Assert.IsTrue(store.MutationCount == 2);
            Assert.IsTrue(store.CurrentMutation == 2);

            versions = store.GetVersionInfo(ent)
                                .Versions
                                .Cast<PropertyEntity<string>>()
                                .ToList();

            Assert.IsTrue(versions.Count == 2);
            Assert.IsTrue(versions[0].Value == "foo");
            Assert.IsTrue(versions[1].Value == "thud");
        }

        [TestMethod]
        public void RevertTest()
        {
            // create a new store
            var store = new EntityStore();

            // commit an entity to the store to track information
            var ent = store.Commit(new PropertyEntity<string>("foo"));
            store.Commit(ent.With("bar"));
            store.Commit(ent.With("baz"));
            store.Commit(ent.With("qux"));

            // give the entity some random value
            ent.Value = "thud";

            var currentMutation = store.CurrentMutation;

            store.Revert(ent);

            // entity should have been reverted to the nearest known version which is qux
            Assert.IsTrue(ent.Value == "qux");
            Assert.IsTrue(currentMutation == store.CurrentMutation);

            while (ent.Value != "bar" && store.Undo()) ;

            ent.Value = "thud";

            currentMutation = store.CurrentMutation;
            store.Revert(ent);

            Assert.IsTrue(ent.Value == "bar");
            Assert.IsTrue(currentMutation == store.CurrentMutation);

            store.Undo();

            ent.Value = "thud";

            currentMutation = store.CurrentMutation;
            store.Revert(ent);

            Assert.IsTrue(ent.Value == "foo");
            Assert.IsTrue(currentMutation == store.CurrentMutation);
        }


        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void RegisterRevertTest()
        {
            var store = new EntityStore();
            var ent = store.Register(new PropertyEntity<string>("foo"));

            store.Revert(ent);
        }


        [TestMethod]
        public void RegisterCommitThenRevertTest()
        {
            var store = new EntityStore();
            var ent = store.Register(new PropertyEntity<string>("foo"));

            // this will add versioning to ent, allowing us to revert down the line
            store.Commit(ent);
            store.Commit(ent.With("bar"));

            ent.Value = "qaz";

            store.Revert(ent);

            Assert.IsTrue(ent.Value == "bar");
        }

        [TestMethod]
        public void DeregisterWithRegisteredEntityTest()
        {
            var store = new EntityStore();
            var ent = store.Register(new PropertyEntity<string>("foo"));

            Assert.IsTrue(store.Contains(ent));
            Assert.IsTrue(store.GetVersionInfo(ent) == null);
            Assert.IsTrue(store.MutationCount == 0);
            Assert.IsTrue(store.EntitiesInScope == 1);

            store.Deregister(ent);

            Assert.IsFalse(store.Contains(ent));
            Assert.IsTrue(store.MutationCount == 0);
            Assert.IsTrue(store.EntitiesInScope == 0);
        }

        [TestMethod]
        public void DeregisterWithCommittedEntityTest()
        {
            var store = new EntityStore();
            var ent = store.Commit(new PropertyEntity<string>("foo"));

            Assert.IsTrue(store.Contains(ent));

            var versionInfo = store.GetVersionInfo(ent);

            Assert.IsTrue(versionInfo != null);
            Assert.IsTrue(store.MutationCount == 1);
            Assert.IsTrue(store.EntitiesInScope == 4);

            store.Deregister(ent);

            Assert.IsFalse(store.Contains(ent));
            // can't get version info anymore...
            Assert.IsTrue(store.GetVersionInfo(ent) == null);

            // ... but it still exists in the store
            Assert.IsTrue(store.Contains(versionInfo));
            Assert.IsTrue(store.MutationCount == 2);

            // ent has been removed but the deregister mutation and exit version
            // has been added
            Assert.IsTrue(store.EntitiesInScope == 5);
        }

        [TestMethod]
        public void DeregisterWithRemovedEntityTest()
        {
            var store = new EntityStore();
            var ent = store.Commit(new PropertyEntity<string>("foo"));

            Assert.IsTrue(store.Contains(ent));

            var versionInfo = store.GetVersionInfo(ent);

            Assert.IsTrue(versionInfo != null);
            Assert.IsTrue(store.MutationCount == 1);
            Assert.IsTrue(store.EntitiesInScope == 4);

            // move back to the point of the first commit (can't move back to the tail
            // because the entity doesn't exist at that point)
            store.Undo();
            store.Deregister(ent);

            Assert.IsFalse(store.Contains(ent));
            // can't get version info anymore...
            Assert.IsTrue(store.GetVersionInfo(ent) == null);

            // ... but it no it doesn't exists in the store
            Assert.IsFalse(store.Contains(versionInfo));
            Assert.IsTrue(store.MutationCount == 0);

            // all is gone
            Assert.IsTrue(store.EntitiesInScope == 0);
        }

        [TestMethod]
        public void GroupTest()
        {
            var store = new EntityStore();
            var ent = store.Commit(new PropertyEntity<string>("foo"));

            store.BeginMutationGroup();

            store.Commit(ent.With("bar"));
            store.Commit(ent.With("qaz"));
            store.Commit(ent.With("thud"));

            store.EndMutationGroup();

            // total number of mutations = 4x commit + begin and end group
            Assert.IsTrue(store.CurrentMutation == 6);

            store.Undo();

            // this will move back to the begin group
            Assert.IsTrue(store.CurrentMutation == 1);
            Assert.IsTrue(ent.Value == "bar");

            store.Redo();

            Assert.IsTrue(store.CurrentMutation == 6);
            Assert.IsTrue(ent.Value == "thud");

        }
    }
}