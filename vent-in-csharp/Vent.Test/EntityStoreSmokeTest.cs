/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Diagnostics;

namespace Vent.Test
{
    [TestClass]
    public partial class EntityStoreSmokeTest
    {
     
        /// <summary>
        /// Baseline test to see if the smoke test does anything
        /// </summary>
        [TestMethod]
        public void RunSingleIterationTest()
        {
            var actions = new List<SmokeTestAction>()
            {
                new SmokeTestAction()
                {
                    UpperBound = 10,
                    Mutation = (store) =>
                    {
                        store.Commit(new PropertyEntity<string>("foo"));
                    }
                }
            };

            SmokeTestAction actionSelection(HistorySystem store) => SmokeTestAction.SelectAction(actions, store, new Random(1));

            var store = RunSmokeTest(1, -1, 42, actionSelection);

            Assert.IsTrue(store.MutationCount == 1);
            Assert.IsTrue(store.Registry.EntitiesInScope == 4);
        }

        /// <summary>
        /// Baseline test to see if the smoke test generates foo/bar/qez randomly
        /// </summary>
        [TestMethod]
        public void EntityGenerationTest()
        {
            var actions = new List<SmokeTestAction>()
            {
                new SmokeTestAction("commit foo", 10, (store) => store.Commit(new PropertyEntity<string>("foo")), _ => true),
                new SmokeTestAction("commit bar", 10, (store) => store.Commit(new PropertyEntity<string>("bar")), _ => true),
                new SmokeTestAction("commit qez", 10, (store) => store.Commit(new PropertyEntity<string>("qez")), _ => true)
            };

            var rng = new Random(1);
            SmokeTestAction actionSelection(HistorySystem store) => SmokeTestAction.SelectAction(actions, store, rng);

            var store = RunSmokeTest(100, -1, 3, actionSelection);

            Assert.IsTrue(store.MutationCount == 100);
            Assert.IsTrue(store.Registry.EntitiesInScope == 400);

            var versionedProperties = store.Registry.Where(kvp => store.HasVersionInfo(kvp.Value) && kvp.Value is PropertyEntity<string>)
                                            .Select(kvp => kvp.Value)
                                            .Cast <PropertyEntity<string>>();

            var fooCount = versionedProperties.Count(e => e.Value == "foo");
            var barCount = versionedProperties.Count(e => e.Value == "bar");
            var qezCount = versionedProperties.Count(e => e.Value == "qez");

            Assert.IsTrue(fooCount > 0);
            Assert.IsTrue(barCount > 0);
            Assert.IsTrue(qezCount > 0);
        }

        [TestMethod]
        public void DefaultActionSetTest()
        {
            var randomSelection = new Random(42);
            var actionSet = SmokeTestAction.CreateDefaultActionSet(randomSelection /*, (str) => Debug.WriteLine(str)*/);

            SmokeTestAction actionSelection(HistorySystem store) => SmokeTestAction.SelectAction(actionSet, store, randomSelection);

            // just try some random tests 
            var store = RunSmokeTest(50000, -1, 28371, actionSelection, true);
            Debug.WriteLine(store.ToStateString());

            store = RunSmokeTest(50000, 50, 3128, actionSelection, true, 150);
            Debug.WriteLine(store.ToStateString());
            Assert.IsTrue(store.Registry.SlotCount < 150);
            Assert.IsTrue(store.MutationCount < 50);

            store = RunSmokeTest(50000, 75, 18162, actionSelection, true, 250);
            Debug.WriteLine(store.ToStateString());
            Assert.IsTrue(store.Registry.SlotCount < 250);
            Assert.IsTrue(store.MutationCount < 75);

            store = RunSmokeTest(50000, 100, 71216, actionSelection, true, 500);
            Debug.WriteLine(store.ToStateString());
            Assert.IsTrue(store.Registry.SlotCount < 500);
            Assert.IsTrue(store.MutationCount < 100);
        }
        
        
       [TestMethod]
        public void GroupActionTest()
        {
            var randomSelection = new Random(42);

            var actionSet = SmokeTestAction
                            .CreateDefaultActionSet(randomSelection/*, str => Debug.WriteLine(str)*/)
                            .AddGroupActions(randomSelection /*, str => Debug.WriteLine(str)*/, maxGroups: 5);

            SmokeTestAction actionSelection(HistorySystem store) => SmokeTestAction.SelectAction(actionSet, store, randomSelection);

            // run various smoke tests which different seeds
            var store = RunSmokeTest(20000, -1, 2837, actionSelection, true, 100);
            Debug.WriteLine(store.ToStateString());

            store = RunSmokeTest(20000, 100, 37261, actionSelection, true);
            Debug.WriteLine(store.ToStateString());

            store = RunSmokeTest(20000, 50, 12961, actionSelection, true, 60);
            Debug.WriteLine(store.ToStateString());

            store = RunSmokeTest(20000, 75, 94848, actionSelection, true, 250);
            Debug.WriteLine(store.ToStateString());
        }


        [TestMethod]
        public void RunRepeatUndoRedoInsertTest()
        {
            var count = 7;
            for (var iterations = 0; iterations < count; iterations++)
            {
                for (var undo = 0; undo < count; undo++)
                {
                    for (var redo = 0; redo < count; redo++)
                    {
                        for (var entityCount = 0; entityCount < count; entityCount++)
                        {
                            for (var maxMutations = 1; maxMutations < count; maxMutations++)
                            {
                                for (var insertCount = 0; insertCount <  count; insertCount++)
                                {
                                    RepeatUndoRedoInsertTest(iterations, entityCount, maxMutations, undo, redo, insertCount, spool: true);

                                    // for debugging
                                    /*
                                    var store = RepeatUndoRedoInsertTest(iterations, entityCount, maxMutations, undo, redo, insertCount, spool: true);
                                    if (entityCount > 3 && maxMutations > 3 && undo > 2 && redo == 0 && iterations >= 2 && insertCount > 0)
                                    {
                                        //Debug.WriteLine(store.ToStateString());
                                    }*/
                                }
                            }
                        }
                    }
                }
            }
        }

        private HistorySystem RunSmokeTest(
            int iterations, 
            int maxMutations, 
            int seed,
            Func<HistorySystem, SmokeTestAction> selectAction,
            bool deleteOutOfScopeVersions = false,
            int maxEntitySlots = int.MaxValue)
        {
            var registry = new EntityRegistry()
            {
                MaxEntitySlots = maxEntitySlots
            };

            var store = new HistorySystem(registry, new EntityHistory(maxMutations, deleteOutOfScopeVersions));
            var rng = new Random(seed);

           // try
            {
                for (var i = 0; i < iterations; i++)
                {
                    var action = selectAction(store);
                    
                    if (action != null)
                    {
                        action.Mutation.Invoke(store);
                    }
                    else
                    {
                        Debug.WriteLine("could not select action, ending ...");
                        break;
                    }
                }

                Debug.WriteLine("Spool store... closing open groups");
                while (store.OpenGroupCount > 0)
                {
                    if (store.Registry.SlotCount >= store.Registry.MaxEntitySlots - 1)
                    {
                        store.DeleteMutation(0);
                    }

                    store.EndMutationGroup();
                }

                store.ToTail();
                store.ToHead();
            }
            /*catch( Exception e)
            {
                Debug.WriteLine("Exception " + e);
                Debug.WriteLine("Store:\n " + store.ToStateString());
                Assert.Fail();
            }*/

            return store;
        }

        private HistorySystem RepeatUndoRedoInsertTest(
            int iterations,
            int entityCount,
            int maxMutations, 
            int undoCount,
            int redoCount,
            int insertCount,
            HistorySystem store= null,
            bool spool = false)
        {
            store ??= new HistorySystem(
                new EntityRegistry(), 
                new EntityHistory()
                {
                    MaxMutations = maxMutations,
                }
            );

            for (var i = 0; i < entityCount; i++)
            {
                store.Commit(new PropertyEntity<string>($"{i}-initial"));
            }

            for (var it = 0; it < iterations; it++)
            {
                for (var i = 0; i < undoCount && store.Undo(); i++)
                {
                }

                for (var i = 0; i < redoCount && store.Redo(); i++)
                {
                }

                for (var i = 0; i < insertCount; i++)
                {
                    store.Commit(new PropertyEntity<string>($"{it}:{i}-inserted"));
                }
            }

            if (spool)
            {
                store.ToTail();
                store.ToHead();
            }

            return store;
        }
    }
}
