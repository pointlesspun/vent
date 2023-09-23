using System.Text.Json;
using Vent.PropertyEntities;

namespace Vent.ToJson.Test
{
    [TestClass]
    public class RegistryConverterTest
    {
        [TestMethod]
        public void SingleStringEntityTest()
        {
            var registry = new EntityRegistry
            {
                new StringEntity("foo")
            };

            var serializeOptions = CreateTestOptions();

            var json = JsonSerializer.Serialize(registry, serializeOptions);

            var clone = JsonSerializer.Deserialize<EntityRegistry>(json, serializeOptions);

            Assert.IsTrue(clone != null);
            Assert.IsTrue(clone.MaxEntitySlots == registry.MaxEntitySlots);
            Assert.IsTrue(clone.NextEntityId == registry.NextEntityId);
            Assert.IsTrue(clone.EntitiesInScope == registry.EntitiesInScope);
            Assert.IsTrue(((StringEntity)clone[0]) != ((StringEntity)registry[0]));
            Assert.IsTrue(((StringEntity)clone[0]).Value == ((StringEntity)registry[0]).Value);
        }

        [TestMethod]
        public void MultipleStringEntityTest()
        {
            var registry = new EntityRegistry
            {
                new StringEntity("foo"),
                new StringEntity("bar"),
                new StringEntity("qaz")
            };

            var serializeOptions = CreateTestOptions();

            var json = JsonSerializer.Serialize(registry, serializeOptions);

            var clone = JsonSerializer.Deserialize<EntityRegistry>(json, serializeOptions);

            Assert.IsTrue(clone != null);
            Assert.IsTrue(clone.MaxEntitySlots == registry.MaxEntitySlots);
            Assert.IsTrue(clone.NextEntityId == registry.NextEntityId);
            Assert.IsTrue(clone.EntitiesInScope == registry.EntitiesInScope);

            foreach (var kvp in registry)
            {
                Assert.IsTrue(((StringEntity)clone[kvp.Key]) != ((StringEntity)registry[kvp.Key]));
                Assert.IsTrue(((StringEntity)clone[kvp.Key]).Value == ((StringEntity)registry[kvp.Key]).Value);
            }
        }

        [TestMethod]
        public void MultipleMultiPropertyEntityTest()
        {
            var registry = new EntityRegistry()
            {
                new MultiPropertyTestEntity(true, "foo", 'c', -42, 42, -42.0f, 42.42),
                new MultiPropertyTestEntity(false, "bar", 'b', -0, 1, float.MaxValue, Double.MinValue),
            };

            var serializeOptions = CreateTestOptions();

            var json = JsonSerializer.Serialize(registry, serializeOptions);

            var clone = JsonSerializer.Deserialize<EntityRegistry>(json, serializeOptions);

            Assert.IsTrue(clone != null);
            Assert.IsTrue(clone.MaxEntitySlots == registry.MaxEntitySlots);
            Assert.IsTrue(clone.NextEntityId == registry.NextEntityId);
            Assert.IsTrue(clone.EntitiesInScope == registry.EntitiesInScope);

            foreach (var kvp in registry)
            {
                Assert.IsTrue(clone[kvp.Key].Equals(registry[kvp.Key]));
            }
        }

        [TestMethod]
        public void EntityReferenceTest()
        {
            var registry = new EntityRegistry();
            var str1 = registry.Add(new StringEntity("foo"));
            registry.Add(new EntityPropertyEntity(str1));

            var str2 = new StringEntity("bar");

            // point to an entity ahead, this will require the converter
            // to delay registration until str2 appears in the store
            registry.Add(new EntityPropertyEntity(str2));
            registry.Add(str2);
            
            // add entity with a null reference
            registry.Add(new EntityPropertyEntity());

            var serializeOptions = CreateTestOptions();

            var json = JsonSerializer.Serialize(registry, serializeOptions);

            var clone = JsonSerializer.Deserialize<EntityRegistry>(json, serializeOptions);

            Assert.IsTrue(clone != null);
            Assert.IsTrue(clone.MaxEntitySlots == registry.MaxEntitySlots);
            Assert.IsTrue(clone.NextEntityId == registry.NextEntityId);
            Assert.IsTrue(clone.EntitiesInScope == registry.EntitiesInScope);

            foreach (var kvp in registry)
            {
                Assert.IsTrue(clone[kvp.Key].Equals(registry[kvp.Key]));
            }
        }

        [TestMethod]
        public void IntListEntityTest()
        {
            var registry = new EntityRegistry()
            {
                new IntListEntity()
                {
                    IntList = new List<int> { 1, 2 },
                }
            };

            var serializeOptions = CreateTestOptions();

            var json = JsonSerializer.Serialize(registry, serializeOptions);

            var clone = JsonSerializer.Deserialize<EntityRegistry>(json, serializeOptions);

            Assert.IsTrue(clone != null);
            Assert.IsTrue(clone.MaxEntitySlots == registry.MaxEntitySlots);
            Assert.IsTrue(clone.NextEntityId == registry.NextEntityId);
            Assert.IsTrue(clone.EntitiesInScope == registry.EntitiesInScope);

            foreach (var kvp in registry)
            {
                Assert.IsTrue(clone[kvp.Key].Equals(registry[kvp.Key]));
                Assert.IsFalse(clone[kvp.Key] == registry[kvp.Key]);
            }
        }

        [TestMethod]
        public void EntityListEntityTest()
        {
            var foo = new StringEntity("foo");
            var bar = new StringEntity("bar");

            var registry = new EntityRegistry()
            {
                foo,
                new EntityListEntity()
                {
                    EntityList = new List<IEntity> { foo, bar }
                },
                bar
            };

            var serializeOptions = CreateTestOptions();

            var json = JsonSerializer.Serialize(registry, serializeOptions);

            var clone = JsonSerializer.Deserialize<EntityRegistry>(json, serializeOptions);

            Assert.IsTrue(clone != null);
            Assert.IsTrue(clone.MaxEntitySlots == registry.MaxEntitySlots);
            Assert.IsTrue(clone.NextEntityId == registry.NextEntityId);
            Assert.IsTrue(clone.EntitiesInScope == registry.EntitiesInScope);

            foreach (var kvp in registry)
            {
                Assert.IsTrue(clone[kvp.Key].Equals(registry[kvp.Key]));
                Assert.IsFalse(clone[kvp.Key] == registry[kvp.Key]);
            }
        }

        // xxx to test
        // - EntityStore in EntityStore (special case)
        // - Entity with Dictionary
        // - Entity with EntityDictionary
        // - Entity with complex object as property
        // - Entity with complex object referencing an entity
        // - Entity with complex object using a list 
        // - Entity with complex object using an entity list 
        // - Entity History property
        // - Attribute indicating an entity property is not in the store and owned by the encapsulating
        //   class
        private JsonSerializerOptions CreateTestOptions()
        {
            var converter = new EntityRegistryConverter(typeof(MultiPropertyTestEntity).Assembly, typeof(StringEntity).Assembly);
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters =
                {
                    converter
                }
            };
        }
    }
}
