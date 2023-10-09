/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using Vent.Registry;
using Vent.ToJson.Readers;

using static Vent.ToJson.Utf8JsonWriterExtensions;

namespace Vent.ToJson.Test.Readers
{
    [TestClass]
    public class DictionaryReaderTest
    {
        [TestMethod]
        public void ReadNullDictionaryTest()
        {
            var dictionary = (Dictionary<string, string>)null;
            var dictionaryString = WriteObjectToJsonString(dictionary);
            var reader = new Utf8JsonDictionaryReader<string, string>();
            Assert.IsNull(reader.ReadFromJson(dictionaryString));
        }

        /// <summary>
        /// Can only use primitives or date
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void ReadEntityIntDictionaryTest()
        {
            var dictionary = new Dictionary<int, int>()
            {
                { 1, 1 },
            };
            var dictionaryString = WriteObjectToJsonString(dictionary);
            var reader = new Utf8JsonDictionaryReader<IEntity, int>();
            reader.ReadFromJson(dictionaryString);
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
            var reader = new Utf8JsonDictionaryReader<int, int>();
            var output = reader.ReadFromJson(dictionaryString);

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
            var reader = new Utf8JsonDictionaryReader<string, string>();
            var output = reader.ReadFromJson(dictionaryString);

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
            var reader = new Utf8JsonDictionaryReader<DateTime, string>();
            var output = reader.ReadFromJson(dictionaryString);

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
            var reader = new Utf8JsonDictionaryReader<string, Dictionary<string, string>>();
            var output = reader.ReadFromJson(dictionaryString);

            foreach (var kvp in dictionary)
            {
                foreach (var innerKvp in kvp.Value)
                {
                    Assert.IsTrue(innerKvp.Value == output[kvp.Key][innerKvp.Key]);
                }
            }
        }

        [TestMethod]
        public void ReadStringListTest()
        {
            var dictionary = new Dictionary<string, List<string>>()
            {
                { "foo", new List<string>() { "innerfoo-key", "foo-value" }},
                { "bar", new List<string>() { "innerbar-key", "bar-value" } },
                { "baz", new List < string > () { "innerbaz-key", "baz-value" }},
            };
            var dictionaryString = WriteObjectToJsonString(dictionary);
            var reader = new Utf8JsonDictionaryReader<string, List<string>>();
            var output = reader.ReadFromJson(dictionaryString);

            foreach (var kvp in dictionary)
            {
                Assert.IsTrue(kvp.Value.SequenceEqual(output[kvp.Key]));
            }
        }
    }
}
