
using System.Text.Json;
using static Vent.ToJson.Readers.Utf8JsonPrimitiveReader;

namespace Vent.ToJson.Test.Readers
{
    [TestClass]
    public class PrimitiveReaderTest
    {
        [TestMethod]
        public void PrimitivesReadTest()
        {
            Assert.IsTrue(ReadPrimitiveFromJson<int>("-42") == -42);
            Assert.IsTrue(ReadPrimitiveFromJson<uint>("42") == 42);
            Assert.IsTrue(ReadPrimitiveFromJson<bool>("true") == true);
            Assert.IsTrue(ReadPrimitiveFromJson<bool>("false") == false);
            Assert.IsTrue(ReadPrimitiveFromJson<char>("\"x\"") == 'x');
            Assert.IsTrue(ReadPrimitiveFromJson<string>("\"foo\"") == "foo");
            Assert.IsTrue(ReadPrimitiveFromJson<float>("1.01") == 1.01f);
            Assert.IsTrue(ReadPrimitiveFromJson<byte>("64") == 64);
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void NotAPrimitiveTest()
        {
            Assert.IsTrue(ReadPrimitiveFromJson<DateTime>($"\"{DateTime.Now.ToString()}\"") == DateTime.Now);
        }

        [TestMethod]
        [ExpectedException(typeof(JsonException))]
        public void NullIsNotAPrimitiveTest()
        {
            Assert.IsTrue(ReadPrimitiveFromJson<string>("null") == "null");
        }
    }
}
