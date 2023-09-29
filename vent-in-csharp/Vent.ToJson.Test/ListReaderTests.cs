using System.Text;
using System.Text.Json;
using Vent.ToJson.Readers;

namespace Vent.ToJson.Test
{
    [TestClass]
    public class ListReaderTests
    {
        [TestMethod]
        public void NullListTest()
        {
            var listString = "null";
            var listReader = new Utf8JsonReader(Encoding.UTF8.GetBytes(listString));
            var listOutput = listReader.ReadList<string>();

            Assert.IsTrue(listOutput == null);
        }

        [TestMethod]
        public void PrimitiveListTest()
        {
            var list = new List<int>() { 1, 2, 3 };
            var listString = $"[{string.Join(",", list)}]";
            var listReader = new Utf8JsonReader(Encoding.UTF8.GetBytes(listString));
            var listOutput = listReader.ReadList<int>();

            Assert.IsTrue(list.SequenceEqual(listOutput));
        }

        [TestMethod]
        public void StringListTest()
        {
            var list = new List<string>() { "foo", "bar", "qaz" };
            var listString = $"[{string.Join(",", list.Select(str => $"\"{str}\""))}]";
            var listReader = new Utf8JsonReader(Encoding.UTF8.GetBytes(listString));
            var listOutput = listReader.ReadList<string>();

            Assert.IsTrue(list.SequenceEqual(listOutput));
        }

        [TestMethod]
        public void BooleanListTest()
        {
            var list = new List<bool>() { true, false };
            var listString = $"[{string.Join(",", list.Select(b => b.ToString().ToLower()))}]";
            var listReader = new Utf8JsonReader(Encoding.UTF8.GetBytes(listString));
            var listOutput = listReader.ReadList<bool>();

            Assert.IsTrue(list.SequenceEqual(listOutput));
        }

        [TestMethod]
        public void NestedListTest()
        {
            var list = new List<List<int>>() 
            { 
                new List<int> { 1, 2, 3 }, 
                new List<int> { 3, 4, 5 },
                new List<int> { 6, 7, 8 }
            };
            var listString = JsonSerializer.Serialize(list);
            var listReader = new Utf8JsonReader(Encoding.UTF8.GetBytes(listString));
            var listOutput = listReader.ReadList<List<int>>();

            for (var i = 0; i < list.Count; i++)
            {
                Assert.IsTrue(list[i].SequenceEqual(listOutput[i]));
            }
        }
    }
}
