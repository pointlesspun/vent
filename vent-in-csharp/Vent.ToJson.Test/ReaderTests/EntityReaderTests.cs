/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using Vent.Entities;
using Vent.Registry;
using Vent.ToJson.Readers;
using Vent.ToJson.Test.TestEntities;

using static Vent.ToJson.Utf8JsonWriterExtensions;

namespace Vent.ToJson.Test.Readers
{
    [TestClass]
    public class EntityReaderTests
    {
        [TestMethod]
        public void ReadNullEntityTest()
        {
            var entityString = WriteObjectToJsonString(null);
            var reader = new Utf8JsonEntityReader();

            Assert.IsNull(reader.ReadFromJson(entityString, entitySerialization: EntitySerialization.AsReference));
            Assert.IsNull(reader.ReadFromJson(entityString, entitySerialization: EntitySerialization.AsValue));
        }

        [TestMethod]
        public void ReadNegativeKeyEntityTest()
        {
            var reader = new Utf8JsonEntityReader();
            
            Assert.IsNull(reader.ReadFromJson("-1", entitySerialization: EntitySerialization.AsReference));
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
            var reader = new Utf8JsonEntityReader();
            var output = reader.ReadFromJson(entityString, context);

            Assert.IsTrue(output.Equals(registry[0]));
            Assert.IsTrue(output == registry[0]);
        }

        /// <summary>
        /// Serialize an entity with a reference to its own registry. Test if 
        /// the registry is correctly restored (by reference).
        /// </summary>
        [TestMethod]
        public void ReadEntityRegistryAsPropertyTest()
        {
            var registry = new EntityRegistry();
            var ent = registry.Add(new ObjectEntity<EntityRegistry>(registry));
            var context = new JsonReaderContext(registry, ClassLookup.CreateDefault());

            // ent needs to be serialized as value, the registry property of ent should be serialized by reference
            var entityString = WriteObjectToJsonString(ent, EntitySerialization.AsValue);
            var reader = new Utf8JsonEntityReader();
            var output = (ObjectEntity<EntityRegistry>) reader.ReadFromJson(entityString, context, EntitySerialization.AsValue);

            Assert.IsTrue(output.Value == registry);
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
            var reader = new Utf8JsonEntityReader();
            var output = reader.ReadFromJson(entityString, context, EntitySerialization.AsValue);

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
            var reader = new Utf8JsonEntityReader();
            reader.ReadFromJson(entityString, context, EntitySerialization.AsValue);

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
            var reader = new Utf8JsonEntityReader();
            var output = (ForwardEntityReference) reader.ReadFromJson(entityString, context);

            Assert.IsTrue(output.EntityId == 0);
            Assert.IsTrue(output.Registry == unloadedRegistry);
        }

        private struct TestStruct
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }


        [TestMethod]
        public void StructReadTest()
        {
            var structObject = new PropertyEntity<TestStruct>(new TestStruct()
            {
                Id = 42,
                Name = "foo"
            });

            var context = new JsonReaderContext(ClassLookup.CreateDefault().WithType(typeof(TestStruct)));

            var entityString = WriteObjectToJsonString(structObject, EntitySerialization.AsValue);
            var reader = new Utf8JsonEntityReader();
            var output = (PropertyEntity<TestStruct>)reader.ReadFromJson(entityString, context, EntitySerialization.AsValue);

            Assert.IsTrue(output.Value.Id == 42);
            Assert.IsTrue(output.Value.Name == "foo");
        }
    }
}

