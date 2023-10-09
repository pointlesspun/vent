/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using Vent.Registry;
using Vent.ToJson.Test.TestEntities;
using Vent.ToJson.Readers;

using static Vent.ToJson.Utf8JsonWriterExtensions;

namespace Vent.ToJson.Test.Readers
{
    [TestClass]
    public class ObjectReaderTests
    {
        [TestMethod]
        public void ReadNullObjectTest()
        {
            var testObject = (MultiPropertyTestObject)null;
            var testString = WriteObjectToJsonString(testObject, EntitySerialization.AsValue);
            var reader = new Utf8JsonObjectReader<MultiPropertyTestObject>();
            var output = reader.ReadFromJson(testString);

            Assert.IsTrue(output == null);
        }


        [TestMethod]
        public void ReadMultiPropertyTestObjectTest()
        {
            var testObject = new MultiPropertyTestObject(true, "foo", 'c', -3, 2, -0.42f, 0.042);
            var testString = WriteObjectToJsonString(testObject, EntitySerialization.AsValue);
            var reader = new Utf8JsonObjectReader<MultiPropertyTestObject>();
            var output = reader.ReadFromJson(testString);

            Assert.IsTrue(testObject.Equals(output));
        }

        [TestMethod]
        [ExpectedException(typeof(EntitySerializationException))]
        public void ReadEntityAsObjectTest()
        {
            var testObject = new ObjectWrapperEntity<MultiPropertyTestObject>(
                new MultiPropertyTestObject(true, "foo", 'c', -3, 2, -0.42f, 0.042));
            var testString = WriteObjectToJsonString(testObject, EntitySerialization.AsValue);
            var reader = new Utf8JsonObjectReader<MultiPropertyTestObject>();

            // ObjectWrapper is an entity so this should throw an exception
            reader.ReadFromJson(testString);
        }

        [TestMethod]
        public void WriteNestedObjectTest()
        {
            var testObject = new TupleObject<MultiPropertyTestObject, int>()
            {
                Item1 = new MultiPropertyTestObject(true, "foo", 'c', -3, 2, -0.42f, 0.042),
                Item2 = 42
            };

            var testString = WriteObjectToJsonString(testObject, EntitySerialization.AsValue);
            var reader = new Utf8JsonObjectReader<TupleObject<MultiPropertyTestObject, int>>();
            var output = reader.ReadFromJson(testString);
            
            Assert.IsTrue(output.Item1.Equals(testObject.Item1));
            Assert.IsTrue(output.Item2 == testObject.Item2);
        }

        // xxx test with tuple <list, dict>
    }
}
