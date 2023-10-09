using Vent.Entities;
using Vent.Registry;
using Vent.ToJson.Test.TestEntities;

using static Vent.ToJson.Utf8JsonWriterExtensions;
using static Vent.ToJson.Readers.Utf8JsonEntityReader;

namespace Vent.ToJson.Test
{
    [TestClass]
    public class ReadMeTests
    {
        [TestMethod]
        public void BasicDeSerializeExample()
        {
            // create a registry with a some random assortments of entities entity
            var registry = new EntityRegistry()
            {
                new StringEntity("foo"),
                new MultiPropertyTestEntity(true, "foo", 'a', -42, 42, 0.1f, -0.1),
                new PropertyEntity<int>(42)
            };

            // write the object to a jsonstring
            var jsonString = WriteRegistryToJson(registry);

            // create an entity reader and read the data from the json string
            var clonedRegistry = ReadEntityFromJson<EntityRegistry>(jsonString);

            Assert.IsTrue(clonedRegistry.Equals(registry));
        }
    }
}
