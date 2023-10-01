using static Vent.ToJson.Utf8JsonWriterExtensions;
using static Vent.ToJson.Readers.Utf8JsonDictionaryReader;

namespace Vent.ToJson.Test
{
    [TestClass]
    public class DictionaryReaderTest
    {
        [TestMethod]
        public void ReadNullDictionaryTest()
        {
            var dictionary = (Dictionary<string, string>)null;
            var dictionaryString = WriteObjectToJsonString(dictionary);
            Assert.IsNull(ReadDictionaryFromJson<string, string>(dictionaryString));
        }

        [TestMethod]
        public void ReadIntIntDictionaryTest()
        {
            var dictionary = new Dictionary<int, int>()
            {
                { -1, 1 },
                { -2, 2 },
                { -3, 3 },
            };
            var dictionaryString = WriteObjectToJsonString(dictionary);
            var output = ReadDictionaryFromJson<int, int>(dictionaryString);

            foreach (var kvp in dictionary)
            {
                Assert.IsTrue(kvp.Value == output[kvp.Key]);
            }
        }

        [TestMethod]
        public void ReadStringStringDictionaryTest()
        {
            var dictionary = new Dictionary<string, string>()
            {
                { "foo1", "bar1" },
                { "foo2", "bar2" },
                { "foo3", "bar3" },
            };
            var dictionaryString = WriteObjectToJsonString(dictionary);
            var output = ReadDictionaryFromJson<string, string>(dictionaryString);

            foreach (var kvp in dictionary)
            {
                Assert.IsTrue(kvp.Value == output[kvp.Key]);
            }
        }

        [TestMethod]
        public void ReadTimeStampStringDictionaryTest()
        {
            var dictionary = new Dictionary<DateTime, string>()
            {
                { new DateTime(1970, 1, 1), "foo" },
                { new DateTime(2000, 2, 2), "bar" },
                { new DateTime(2030, 3, 3), "baz" },
            };
            var dictionaryString = WriteObjectToJsonString(dictionary);
            var output = ReadDictionaryFromJson<DateTime, string>(dictionaryString);

            foreach (var kvp in dictionary)
            {
                Assert.IsTrue(kvp.Value == output[kvp.Key]);
            }
        }

        [TestMethod]
        public void ReadStringDictionaryTest()
        {
            var dictionary = new Dictionary<string, Dictionary<string, string>>()
            {
                { "foo", new Dictionary<string, string>() {{ "innerfoo-key", "foo-value" }}},
                { "bar", new Dictionary<string, string>() {{ "innerbar-key", "bar-value" } }},
                { "baz", new Dictionary<string, string>() {{ "innerbaz-key", "baz-value" }}},
            };
            var dictionaryString = WriteObjectToJsonString(dictionary);
            var output = ReadDictionaryFromJson<string, Dictionary<string, string>>(dictionaryString);

            foreach (var kvp in dictionary)
            {
                foreach (var innerKvp in kvp.Value)
                {
                    Assert.IsTrue(innerKvp.Value == output[kvp.Key][innerKvp.Key]);
                }
            }
        }
    }
}
