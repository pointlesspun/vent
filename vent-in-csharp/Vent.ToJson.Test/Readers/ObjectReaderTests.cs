using Vent.ToJson.Test.TestEntities;

using static Vent.ToJson.Utf8JsonWriterExtensions;
using static Vent.ToJson.Readers.Utf8JsonObjectReader;

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
            var objectOutput = ReadObjectFromJson<MultiPropertyTestObject>(testString);

            Assert.IsTrue(objectOutput == null);
        }


        [TestMethod]
        public void ReadMultiPropertyTestObjectTest()
        {
            var testObject = new MultiPropertyTestObject(true, "foo", 'c', -3, 2, -0.42f, 0.042);
            var testString = WriteObjectToJsonString(testObject, EntitySerialization.AsValue);
            var objectOutput = ReadObjectFromJson<MultiPropertyTestObject>(testString);

            Assert.IsTrue(testObject.Equals(objectOutput));
        }

        [TestMethod]
        [ExpectedException(typeof(EntitySerializationException))]
        public void ReadEntityAsObjectTest()
        {
            var testObject = new ObjectWrapperEntity<MultiPropertyTestObject>(
                new MultiPropertyTestObject(true, "foo", 'c', -3, 2, -0.42f, 0.042));
            var testString = WriteObjectToJsonString(testObject, EntitySerialization.AsValue);

            // ObjectWrapper is an entity so this should throw an exception
            ReadObjectFromJson<ObjectWrapperEntity<MultiPropertyTestObject>>(testString);
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
            var objectOutput = ReadObjectFromJson<TupleObject<MultiPropertyTestObject, int>>(testString);

            Assert.IsTrue(objectOutput.Item1.Equals(testObject.Item1));
            Assert.IsTrue(objectOutput.Item2 == testObject.Item2);
        }

        // xxx test with tuple <list, dict>
    }
}
