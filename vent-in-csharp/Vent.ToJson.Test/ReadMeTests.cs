using Vent.Entities;
using Vent.Registry;
using Vent.ToJson.Test.TestEntities;

using static Vent.ToJson.Writers.Utf8JsonWriterExtensions;
using static Vent.ToJson.Readers.Utf8JsonEntityReader;
using Vent.ToJson.Readers;
using Vent.ToJson.ClassResolver;

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
                new PropertyEntity<int>(42),
                new ObjectWrapperEntity<StringEntity>(new StringEntity("bar"))
            };

            // write the object to a jsonstring
            var jsonString = WriteRegistryToJson(registry);

            // create an entity reader and read the data from the json string
            var clonedRegistry = ReadEntityFromJson<EntityRegistry>(jsonString);

            Assert.IsTrue(clonedRegistry.Equals(registry));
        }

        [TestMethod]
        public void ClassMapExample()
        {
            // create a registry with a some random assortments of entities entity
            var registry = new EntityRegistry()
            {
                new StringEntity("foo"),
                new MultiPropertyTestEntity(true, "foo", 'a', -42, 42, 0.1f, -0.1),
                new PropertyEntity<int>(42),
                new ObjectWrapperEntity<StringEntity>(new StringEntity("bar"))
            };

            // write the object to a jsonstring
            var jsonString = WriteRegistryToJson(registry);

            var classLookup = ClassLookup.CreateFrom(typeof(MultiPropertyTestEntity).Assembly, typeof(StringEntity).Assembly)
                                 .WithType(typeof(Dictionary<,>))
                                 .WithType(typeof(List<>));

            var context = new JsonReaderContext(registry, classLookup);
            var reader = new Utf8JsonEntityReader();
            var clonedRegistry = reader.ReadFromJson(jsonString, context, EntitySerialization.AsValue);

            Assert.IsTrue(clonedRegistry.Equals(registry));
        }
    }
}
