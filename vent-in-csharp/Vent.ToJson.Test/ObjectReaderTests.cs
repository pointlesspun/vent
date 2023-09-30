using Vent.ToJson.Test.TestEntities;

using static Vent.ToJson.Utf8JsonWriterExtensions;
using static Vent.ToJson.Readers.Utf8JsonObjectReader;

namespace Vent.ToJson.Test
{
    [TestClass]
    public class ObjectReaderTests
    {
        [TestMethod]
        public void WriteNullObjectTest()
        {
            var testObject = (MultiPropertyTestObject) null;
            var testString = WriteObjectToJsonString(testObject, EntitySerialization.AsValue);
            var objectOutput = ReadObjectFromJson<MultiPropertyTestObject>(testString);

            Assert.IsTrue(objectOutput == null);
        }


        [TestMethod]
        public void WriteMultiPropertyTestObjectTest()
        {
            var testObject = new MultiPropertyTestObject(true, "foo", 'c', -3, 2, -0.42f, 0.042);
            var testString = WriteObjectToJsonString(testObject, EntitySerialization.AsValue);
            var objectOutput = ReadObjectFromJson< MultiPropertyTestObject>(testString);

            Assert.IsTrue(testObject.Equals(objectOutput));
        }
    }
}
