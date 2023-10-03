using Vent.ToJson.Readers;
using Vent.ToJson.Test.TestEntities;

using static Vent.ToJson.Utf8JsonWriterExtensions;

namespace Vent.ToJson.Test.Readers
{
    [TestClass]
    public class ArrayReaderTests
    {
        [TestMethod]
        public void ReadNullArrayTest()
        {
            var reader = new Utf8JsonArrayReader<int>();
            var output = reader.ReadFromJson("null");

            Assert.IsNull(output);
        }

        [TestMethod]
        public void ReadPrimitiveArrayTest()
        {
            var array = new int[] { 1, 2, 3 };
            var jsonString = WriteObjectToJsonString(array);
            var reader = new Utf8JsonArrayReader<int>();
            var output = reader.ReadFromJson(jsonString);

            Assert.IsTrue(output.SequenceEqual(array));
        }

        [TestMethod]
        public void ReadObjectArrayTest()
        {
            var array = new MultiPropertyTestObject[] 
            {
                new MultiPropertyTestObject(true, "foo", 'a', -1, 1, 0.42f, -0.42),
                new MultiPropertyTestObject(true, "bar", 'b', -2, 2, 0.42f, -0.42),
                new MultiPropertyTestObject(true, "baz", 'c', -3, 3, 0.42f, -0.42)
            };

            var jsonString = WriteObjectToJsonString(array);
            var reader = new Utf8JsonArrayReader<MultiPropertyTestObject>();
            var output = reader.ReadFromJson(jsonString);

            Assert.IsTrue(output.SequenceEqual(array));
        }

        [TestMethod]
        public void ReadNestedArrayTest()
        {
            var array = new int[][]
            {
                new int[]{ 1, 2, 3 },
                new int[]{ 4, 5, 6 },
                new int[]{ 7, 8, 9 },
            };

            var jsonString = WriteObjectToJsonString(array);
            var reader = new Utf8JsonArrayReader<int[]>();
            var output = reader.ReadFromJson(jsonString);

            Assert.IsTrue(output.Length ==  array.Length);

            for (var i = 0; i < output.Length; i++) 
            {
                Assert.IsTrue(output[i].SequenceEqual(array[i]));
            }
        }
    }
}
