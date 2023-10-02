using Vent.ToJson.Test.TestEntities;
using static Vent.ToJson.Utf8JsonWriterExtensions;
using static Vent.ToJson.Readers.Utf8JsonEntityReader;

namespace Vent.ToJson.Test
{
    [TestClass]
    public class EntityReaderTests
    {
        [TestMethod]
        public void ReadNullEntityTest()
        {
            var entityString = WriteObjectToJsonString(null);
            Assert.IsNull(ReadEntityFromJson<MultiPropertyTestEntity>(entityString, entitySerialization:EntitySerialization.AsReference));
            Assert.IsNull(ReadEntityFromJson<MultiPropertyTestEntity>(entityString, entitySerialization: EntitySerialization.AsValue));
        }

        [TestMethod]
        public void ReadPropertyEntityAsReferenceTest()
        {
            var registry = new EntityRegistry()
            {
                new MultiPropertyTestEntity(true, "foo", 'a', -42, 42, 0.1f, -0.1)
            };
            var context = new JsonReaderContext(registry, ClassLookup.CreateDefault());

            var entityString = WriteObjectToJsonString(registry[0]);
            var output = ReadEntityFromJson<MultiPropertyTestEntity>(entityString, context, EntitySerialization.AsReference);

            Assert.IsTrue(output.Equals(registry[0]));
            Assert.IsTrue(output == registry[0]);
        }

        [TestMethod]
        public void ReadPropertyEntityAsValueTest()
        {
            var registry = new EntityRegistry()
            {
                new MultiPropertyTestEntity(true, "foo", 'a', -42, 42, 0.1f, -0.1)
            };
            var context = new JsonReaderContext(registry, ClassLookup.CreateDefault());

            var entityString = WriteObjectToJsonString(registry[0], EntitySerialization.AsValue);
            var output = ReadEntityFromJson<MultiPropertyTestEntity>(entityString, context, EntitySerialization.AsValue);

            Assert.IsTrue(output.Equals(registry[0]));
            Assert.IsTrue(output != registry[0]);
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void ReadPropertyEntityAsValueWithoutBackingClassTest()
        {
            var registry = new EntityRegistry()
            {
                new MultiPropertyTestEntity(true, "foo", 'a', -42, 42, 0.1f, -0.1)
            };
            // the class lookup will not have MultiPropertyTestEntity
            var context = new JsonReaderContext(registry, ClassLookup.CreateFrom(typeof(IEntity).Assembly));

            var entityString = WriteObjectToJsonString(registry[0], EntitySerialization.AsValue);
            ReadEntityFromJson<MultiPropertyTestEntity>(entityString, context, EntitySerialization.AsValue);

            Assert.Fail();
        }

        [TestMethod]
        public void ReadPropertyEntityAsForwardReferenceTest()
        {
            var registry = new EntityRegistry()
            {
                new MultiPropertyTestEntity(true, "foo", 'a', -42, 42, 0.1f, -0.1)
            };
            // replace the entity registry with an empty one 
            var unloadedRegistry = new EntityRegistry();
            var context = new JsonReaderContext(unloadedRegistry, ClassLookup.CreateDefault());

            var entityString = WriteObjectToJsonString(registry[0], EntitySerialization.AsReference);
            var output = ReadEntityFromJson<ForwardReference>(entityString, context, EntitySerialization.AsReference);

            Assert.IsTrue(output.EntityId == 0);
            Assert.IsTrue(output.Registry == unloadedRegistry);
        }
    }
}
