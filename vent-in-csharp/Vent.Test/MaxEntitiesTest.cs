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
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry)
            {
                MaxEntitySlots = 3
            };

            while (registry.SlotCount < store.MaxEntitySlots)
            {
                registry.Register(new PropertyEntity<string>("foo"));
            }

            Assert.IsTrue(registry.SlotCount == 3);
            Assert.IsTrue(registry.EntitiesInScope == 3);

            store.Deregister(registry[1]);
            
            Assert.IsTrue(registry.SlotCount == 2);
            Assert.IsTrue(registry.EntitiesInScope == 2);

            // this should go into id = 1
            var ent = registry.Register(new PropertyEntity<string>("bar"));

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
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry)
            {
                MaxEntitySlots = 6
            };

            var ent = store.Commit(new PropertyEntity<string>("foo"));

            Assert.IsTrue(registry.SlotCount == 4);
            Assert.IsTrue(registry.EntitiesInScope == 4);

            store.Deregister(ent);

            // should contain 2 mutations, 1 version info and 2 versions and 1 slot in reserve
            Assert.IsTrue(registry.SlotCount == 6);
            Assert.IsTrue(registry.EntitiesInScope == 5);

            // everything is in use, this should throw an exception
            registry.Register(new PropertyEntity<string>("bar"));
        }
    }
}
