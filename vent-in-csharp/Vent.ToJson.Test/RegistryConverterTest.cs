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

            var converter = new EntityRegistryConverter(typeof(IEntity).Assembly, typeof(StringEntity).Assembly);
            var serializeOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters =
                {
                    converter
                }
            };

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

            var converter = new EntityRegistryConverter(typeof(IEntity).Assembly, typeof(StringEntity).Assembly);
            var serializeOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters =
                {
                    converter
                }
            };

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
    }
}
