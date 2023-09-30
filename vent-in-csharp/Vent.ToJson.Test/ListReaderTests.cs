using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Vent.PropertyEntities;
using Vent.ToJson.Readers;
using Vent.ToJson.Test.TestEntities;

using static Vent.ToJson.Utf8JsonWriterExtensions;

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

        [TestMethod]
        public void ObjectTest()
        {
            var list = new List<MultiPropertyTestObject>()
            {
                new MultiPropertyTestObject(true, "foo", 'a', -1, 0, -0.40f, 0.040),
                null,
                new MultiPropertyTestObject(false, "bar", 'b', -2, 1, -0.41f, 0.041),
                null,
                new MultiPropertyTestObject(true, "foo", 'c', -3, 2, -0.42f, 0.042),
            };
            var listString = JsonSerializer.Serialize(list);
            var listReader = new Utf8JsonReader(Encoding.UTF8.GetBytes(listString));
            var listOutput = listReader.ReadList<MultiPropertyTestObject>(entitySerialization: EntitySerialization.AsValue);

            Assert.IsTrue(list.SequenceEqual(listOutput));  
        }

        [TestMethod]
        public void EntityValueListTest()
        {
            var context = new JsonReaderContext(
                new EntityRegistry()
                {
                    new StringEntity("foo"),
                    new StringEntity("bar"),
                    new StringEntity("qad"),
                }, 
                ClassLookup.CreateDefault()
            );

            var list = new List<IEntity>()
            {
                new ObjectWrapper<IEntity>(context.TopRegistry[0]),
                null,
                new ObjectWrapper<IEntity>(context.TopRegistry[1]),
                null,
                new ObjectWrapper<IEntity>(context.TopRegistry[2])
            };

            var listString = WriteObjectToJsonString(list, EntitySerialization.AsValue);
            var listOutput = Utf8JsonListReader.ReadListFromJson<IEntity>(listString, context, EntitySerialization.AsValue);
            
            Assert.IsTrue(list.SequenceEqual(listOutput));
        }

        [TestMethod]
        public void EntityReferenceTest()
        {
            var context = new JsonReaderContext(
                new EntityRegistry()
                {
                    new StringEntity("foo"),
                    new StringEntity("bar"),
                    new StringEntity("qad"),
                },
                ClassLookup.CreateDefault()
            );

            var list = new List<IEntity>()
            {
                new ObjectEntity<IEntity>(context.TopRegistry[0]),
                new ObjectEntity<IEntity>(context.TopRegistry[1]),
                new ObjectEntity<IEntity>(context.TopRegistry[2]),
            };

            var listString = WriteObjectToJsonString(list, EntitySerialization.AsValue);
            var listOutput = Utf8JsonListReader.ReadListFromJson<IEntity>(listString, context, EntitySerialization.AsValue);

            Assert.IsTrue(list.SequenceEqual(listOutput));
        }

        [TestMethod]
        public void NestedEntityValueListTest()
        {
            var registry = new EntityRegistry()
            {
                new StringEntity("foo"),
                new StringEntity("bar"),
                new StringEntity("qad"),
            };

            var context = new JsonReaderContext(ClassLookup.CreateDefault());

            var list = new List<List<IEntity>>()
            {
                new List<IEntity>()
                {
                    new ObjectWrapper<IEntity>(registry[0]),
                    new ObjectWrapper<IEntity>(registry[1]),
                },
                new List<IEntity>()
                {
                    new ObjectWrapper<IEntity>(registry[0]),
                    new ObjectWrapper<IEntity>(registry[2]),
                },
                new List<IEntity>()
                {
                    new ObjectWrapper<IEntity>(registry[1]),
                    new ObjectWrapper<IEntity>(registry[2]),
                },
            };

            var listString = WriteObjectToJsonString(list, EntitySerialization.AsValue);
            var listOutput = Utf8JsonListReader.ReadListFromJson<List<IEntity>>(listString, context, EntitySerialization.AsValue);

            for (var i = 0; i < list.Count; i++)
            {
                Assert.IsTrue(list[i].SequenceEqual(listOutput[i]));
            }
        }       
    }
}
