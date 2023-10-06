/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

namespace Vent.Test
{
    [TestClass]
    public class MaxEntitiesTest
    {
        [TestMethod]
        public void SmallSetWithRegisteredEntitiesOnly()
        {
            var registry = new EntityRegistry()
            {
                MaxEntitySlots = 3
            };

            var store = new HistorySystem(registry);
            

            while (registry.SlotCount < registry.MaxEntitySlots)
            {
                registry.Add(new PropertyEntity<string>("foo"));
            }

            Assert.IsTrue(registry.SlotCount == 3);
            Assert.IsTrue(registry.EntitiesInScope == 3);

            store.Deregister(registry[1]);
            
            Assert.IsTrue(registry.SlotCount == 2);
            Assert.IsTrue(registry.EntitiesInScope == 2);

            // this should go into id = 1
            var ent = registry.Add(new PropertyEntity<string>("bar"));

            Assert.IsTrue(ent.Id == 1);

            Assert.IsTrue(registry.SlotCount == 3);
            Assert.IsTrue(registry.EntitiesInScope == 3);
        }

        /// <summary>
        /// Commit an entity then deregister it. It's slot should stay occupied 
        /// so registering a new entity should throw an exception
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DeregisterComittedEntityTest()
        {
            var registry = new EntityRegistry(maxSlots: 7);
            var history = new HistorySystem(registry);

            var ent = history.Commit(new PropertyEntity<string>("foo"));

            Assert.IsTrue(registry.SlotCount == 5);
            Assert.IsTrue(registry.EntitiesInScope == 5);

            history.Deregister(ent);

            // should contain 2 mutations, 1 version info and 2 versions and 1 slot in reserve
            Assert.IsTrue(registry.SlotCount == 7);
            Assert.IsTrue(registry.EntitiesInScope == 6);

            // everything is in use, this should throw an exception
            registry.Add(new PropertyEntity<string>("bar"));
        }
    }
}
