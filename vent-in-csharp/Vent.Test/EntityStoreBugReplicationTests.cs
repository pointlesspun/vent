﻿/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using Vent.Entities;
using Vent.History;
using Vent.Registry;

namespace Vent.Test
{


    [TestClass]
    public class EntityStoreBugReplicationTests
    {
        /* replicate:
     *  commit id:594 = 364.qez
        undo -> 130/130
        update 129 -> 68.foou
        commit id:601 = 367.qez
        update 123 -> 64.qezuu
        undo -> 132/132
        undo -> 131/132
            131/132 => 10:47:1 Commit 123
        redo -> 130/132 
            10:47:1 Commit 601
    */
        [TestMethod]

        public void ReplicateUndoRedoBug()
        {
            var store = new HistorySystem(new EntityRegistry());
            // 64.qezu = a
            var ent0 = store.Commit(new PropertyEntity<string>("a"));
            // 367.qez = b
            var ent1 = store.Commit(new PropertyEntity<string>("b"));
            var v0 = store.GetVersionInfo(ent0);
            var v1 = store.GetVersionInfo(ent1);

            Assert.IsTrue(v0.Versions.Count == 1);
            Assert.IsTrue(v1.Versions.Count == 1);

            ent0.Value += "u";
            store.Commit(ent0);

            // S3
            Assert.IsTrue(v0.Versions.Count == 2);
            Assert.IsTrue(v0.CurrentVersion == 2);
            Assert.IsTrue(v1.Versions.Count == 1);
            Assert.IsTrue(v1.CurrentVersion == 1);
            Assert.IsTrue(store.CurrentMutation == 3);
            Assert.IsTrue(ent0.Value == "au");
            Assert.IsTrue(ent1.Value == "b");

            Assert.IsTrue(store.Undo());

            // S2
            Assert.IsTrue(v0.Versions.Count == 2);
            Assert.IsTrue(v0.CurrentVersion == 1);
            Assert.IsTrue(v1.Versions.Count == 1);
            Assert.IsTrue(v1.CurrentVersion == 1);
            Assert.IsTrue(store.CurrentMutation == 2);
            Assert.IsTrue(ent0.Value == "au");
            Assert.IsTrue(ent1.Value == "b");

            Assert.IsTrue(store.Undo());

            // S1
            Assert.IsTrue(v0.Versions.Count == 2);
            Assert.IsTrue(v0.CurrentVersion == 1);
            Assert.IsTrue(v1.Versions.Count == 1);
            Assert.IsTrue(v1.CurrentVersion == 0);
            Assert.IsTrue(store.CurrentMutation == 1);
            Assert.IsTrue(ent0.Value == "au");
            Assert.IsTrue(ent1.Value == "b");

            Assert.IsTrue(store.Redo());

            // S2
            Assert.IsTrue(v0.Versions.Count == 2);
            Assert.IsTrue(v0.CurrentVersion == 1);
            Assert.IsTrue(v1.Versions.Count == 1);
            Assert.IsTrue(v1.CurrentVersion == 1);
            Assert.IsTrue(store.CurrentMutation == 2);
            Assert.IsTrue(ent0.Value == "au");
            Assert.IsTrue(ent1.Value == "b");
        }

        [TestMethod]
        public void GroupBugEndUndoTest()
        {
            var store = new HistorySystem(new EntityRegistry());
            store.BeginMutationGroup();
            store.EndMutationGroup();
            // this would cause an exception
            store.Undo();
        }

        /*
         * Replicate
         * undo many (5) -> new mutation index: -1
         * Remove oldest 0/1 mutation(s)
         * redo many (5) -> new mutation index: 4
         */
        [TestMethod]
        public void RedoWithDeregisterAtSlot0Test()
        {
            var history = new HistorySystem(new EntityRegistry());
            var ent = history.Commit(new StringEntity("foo"));

            history.Deregister(ent);
            history.ToTail();
            history.DeleteMutation(0);
            
            // first redo just moves the mutation to 0
            history.Redo();

            // this tries to redo deregister but the entity currently does not exist
            // in the store because of the commit which #1 during the undo
            // removed its entity from scope #2 then got deleted
            history.Redo();
        }
    }
}
