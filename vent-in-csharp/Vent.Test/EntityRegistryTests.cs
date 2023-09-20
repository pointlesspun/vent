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

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SelfRegisterTest()
        {
            var registry = new EntityRegistry();
            registry.Register(registry);

            Assert.Fail();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SelfSameEntityTest()
        {
            var registry = new EntityRegistry();
            var ent = registry.Register(new StringEntity("foo"));

            registry.Register(ent);

            Assert.Fail();
        }

        [TestMethod]
        public void WrapEntityIdTest()
        {
            var registry = new EntityRegistry()
            {
                MaxEntitySlots = 2
            };
            var ent1 = registry.Register(new StringEntity("foo"));
            var ent2 = registry.Register(new StringEntity("bar"));

            registry.Deregister(ent1);

            var ent3 = registry.Register(new StringEntity("qaz"));

            Assert.IsTrue(registry.SlotCount == 2);
            Assert.IsTrue(registry.EntitiesInScope == 2);
            Assert.IsTrue(registry.NextEntityId == 1);

            Assert.IsTrue(ent1.Id == -1);
            Assert.IsTrue(registry[ent1.Id] == null);
            Assert.IsTrue(ent3.Id == 0);
            Assert.IsTrue(ent2.Id == 1);
        }

        [TestMethod]
        public void SetNextEntityIdAndWrapEntityIdTest()
        {
            var registry = new EntityRegistry()
            {
                MaxEntitySlots = 3
            };
            var ent1 = registry.Register(new StringEntity("foo"));
            var ent2 = registry.Register(new StringEntity("bar"));
            var ent3 = registry.Register(new StringEntity("qad"));

            registry.Deregister(ent1);

            registry.NextEntityId = 1;

            var ent4 = registry.Register(new StringEntity("thud"));

            Assert.IsTrue(registry.SlotCount == 3);
            Assert.IsTrue(registry.EntitiesInScope == 3);
            Assert.IsTrue(registry.NextEntityId == 1);

            Assert.IsTrue(ent1.Id == -1);
            Assert.IsTrue(registry[ent1.Id] == null);
            Assert.IsTrue(ent4.Id == 0);
            Assert.IsTrue(ent2.Id == 1);
            Assert.IsTrue(ent3.Id == 2);
        }

        [TestMethod]
        public void AssignToSlotTest()
        {
            var registry = new EntityRegistry();
            var ent1 = registry.Register(new StringEntity("foo"));
            var ent2 = registry.AssignEntityToSlot(new StringEntity("bar"), ent1.Id);

            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsTrue(registry.Contains(ent2));
            Assert.IsTrue(ent1.Id == -1);
        }

        [TestMethod]
        public void RemoveFromSlotTest()
        {
            var registry = new EntityRegistry();
            var ent1 = registry.Register(new StringEntity("foo"));

            Assert.IsTrue(registry.Contains(ent1));
            Assert.IsTrue(registry.EntitiesInScope == 1);
            Assert.IsTrue(registry.SlotCount == 1);

            registry.RemoveEntityFromSlot(ent1);

            Assert.IsFalse(registry.Contains(ent1));
            Assert.IsTrue(ent1.Id == -1);
            Assert.IsTrue(registry.EntitiesInScope == 0);
            Assert.IsTrue(registry.SlotCount == 1);
        }

        [TestMethod]
        public void CloneTest()
        {
            var registry = new EntityRegistry()
            {
                MaxEntitySlots = 5,
            };
            
            registry.Register(new StringEntity("foo"));
            registry.Register(new StringEntity("bar"));
            registry.Register(new StringEntity("qad"));

            var clone = registry.Clone() as EntityRegistry;

            Assert.IsTrue(clone.MaxEntitySlots == registry.MaxEntitySlots);

            foreach (var kvp in registry)
            {
                Assert.IsTrue(clone[kvp.Key] != registry[kvp.Key]);
                Assert.IsTrue(((StringEntity)clone[kvp.Key]).Value == ((StringEntity)registry[kvp.Key]).Value);
            }
        }
    }
}
