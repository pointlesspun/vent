using Vent.PropertyEntities;

namespace Vent.Test
{
    [TestClass]
    public class EntityRegistryTests
    {
        [TestMethod]
        public void ConstructorTest()
        {
            var registry = new EntityRegistry();

            Assert.IsTrue(registry.MaxEntitySlots == int.MaxValue);
            Assert.IsTrue(registry.SlotCount == 0);
            Assert.IsTrue(registry.EntitiesInScope == 0);
            Assert.IsTrue(registry.NextEntityId == 0);
        }

        [TestMethod]
        public void RegisterTest()
        {
            var registry = new EntityRegistry();
            var ent = registry.Register(new StringEntity("foo"));
            
            Assert.IsNotNull(ent);
            Assert.IsTrue(registry.SlotCount == 1);
            Assert.IsTrue(registry.EntitiesInScope == 1);
            Assert.IsTrue(registry.NextEntityId == 1);

            Assert.IsTrue(registry[0] == ent);
            Assert.IsTrue(ent.Id == 0);
        }

        [TestMethod]
        public void DeregisterTest()
        {
            var registry = new EntityRegistry();
            var ent = registry.Register(new StringEntity("foo"));

            registry.Deregister(ent);

            Assert.IsTrue(registry.SlotCount == 0);
            Assert.IsTrue(registry.EntitiesInScope == 0);
            Assert.IsTrue(registry.NextEntityId == 1);

            Assert.IsTrue(registry[0] == null);
            Assert.IsTrue(ent.Id == -1);

        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void OutOfMemoryTest()
        {
            var registry = new EntityRegistry()
            {
                MaxEntitySlots = 3
            };

            for (var i = 0; i < 4; i++)
            {
                registry.Register(new StringEntity("foo" + i));
            }

            Assert.Fail();
        }
    }
}
