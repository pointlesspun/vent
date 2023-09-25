using Microsoft.Win32;
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

            CloneAndTest(registry);
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

            CloneAndTest(registry);
        }

        [TestMethod]
        public void MultipleMultiPropertyEntityTest()
        {
            var registry = new EntityRegistry()
            {
                new MultiPropertyTestEntity(true, "foo", 'c', -42, 42, -42.0f, 42.42),
                new MultiPropertyTestEntity(false, "bar", 'b', -0, 1, float.MaxValue, Double.MinValue),
            };

            CloneAndTest(registry);
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

            CloneAndTest(registry);
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

            CloneAndTest(registry);
        }

        /// <summary>
        /// Test an Entity list with both backward and forward entity references
        /// </summary>
        [TestMethod]
        public void EntityListEntityTest()
        {
            // backward reference - ie foo is registered before the list, so it
            // will exist when the list is deserialized            
            var foo = new StringEntity("foo");

            // forward reference - ie bar is registered after the list, so it
            // will not exist when the list is deserialized and will need
            // to be resolved after the deserialization
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

            CloneAndTest(registry);
        }

        /// <summary>
        /// Test cloning a registry containing an object which has an entity with an entity property
        /// marked as serialize as value rather than the default (serialize as reference)
        /// </summary>
        [TestMethod]
        public void ObjectWrapperTest()
        {
            var multiPropertyEntity = new MultiPropertyTestEntity(true, "foo", 'x', -42, 43, 0.1f, -0.1);
            CloneAndTest(new EntityRegistry()
            {
                // ObjectWrapper's value is defined as [SerializeAsValue], so
                // the MultiPropertyTestEntity will be fully serialized as a value
                new ObjectWrapper<MultiPropertyTestEntity>(multiPropertyEntity),
                new ObjectWrapper<MultiPropertyTestEntity>()
            });
        }

        // xxx to test
        // - EntityStore in EntityStore (special case)
        // - Entity with Dictionary
        // - Entity with EntityDictionary
        // - Entity with complex object referencing an entity
        // - Entity with complex object using a list 
        // - Entity with complex object using an entity list 
        // - Entity History property
        private JsonSerializerOptions CreateTestOptions()
        {
            var classLookup = ClassLookup.CreateFrom(typeof(MultiPropertyTestEntity).Assembly, typeof(StringEntity).Assembly);
            var converter = new EntityRegistryConverter(classLookup);
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters =
                {
                    converter
                }
            };
        }

        private void CloneAndTest(EntityRegistry registry)
        {
            var serializeOptions = CreateTestOptions();
            var json = JsonSerializer.Serialize(registry, serializeOptions);
            var clone = JsonSerializer.Deserialize<EntityRegistry>(json, serializeOptions);

            AssertEqualsButNotCopies(registry, clone);
        }

        private void AssertEqualsButNotCopies(EntityRegistry source, EntityRegistry clone)
        {
            Assert.IsTrue(source != null);
            Assert.IsTrue(clone != null);
            Assert.IsTrue(clone != source);
            Assert.IsTrue(clone.MaxEntitySlots == source.MaxEntitySlots);
            Assert.IsTrue(clone.NextEntityId == source.NextEntityId);
            Assert.IsTrue(clone.EntitiesInScope == source.EntitiesInScope);

            foreach (var kvp in source)
            {
                Assert.IsTrue(clone[kvp.Key].Equals(source[kvp.Key]));
                Assert.IsFalse(clone[kvp.Key] == source[kvp.Key]);
            }
        }
    }
}
