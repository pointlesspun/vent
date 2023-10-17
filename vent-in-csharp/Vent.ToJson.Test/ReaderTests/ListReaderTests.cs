/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Text.Json;

using Vent.Entities;
using Vent.Registry;
using Vent.ToJson.ClassResolver;
using Vent.ToJson.Readers;
using Vent.ToJson.Test.TestEntities;

using static Vent.ToJson.Writers.Utf8JsonWriterExtensions;

namespace Vent.ToJson.Test.Readers
{
    [TestClass]
    public class ListReaderTests
    {
        [TestMethod]
        public void NullListTest()
        {
            var listString = "null";
            var reader = new Utf8JsonListReader<string>();
            var output = reader.ReadFromJson(listString);

            Assert.IsTrue(output == null);
        }

        [TestMethod]
        public void PrimitiveListTest()
        {
            var list = new List<int>() { 1, 2, 3 };
            var listString = $"[{string.Join(",", list)}]";
            var reader = new Utf8JsonListReader<int>();
            var output = reader.ReadFromJson(listString);

            Assert.IsTrue(list.SequenceEqual(output));
        }

        [TestMethod]
        public void StringListTest()
        {
            var list = new List<string>() { "foo", "bar", "qaz" };
            var listString = $"[{string.Join(",", list.Select(str => $"\"{str}\""))}]";
            var reader = new Utf8JsonListReader<string>();
            var output = reader.ReadFromJson(listString);

            Assert.IsTrue(list.SequenceEqual(output));
        }

        [TestMethod]
        public void BooleanListTest()
        {
            var list = new List<bool>() { true, false };
            var listString = $"[{string.Join(",", list.Select(b => b.ToString().ToLower()))}]";
            var reader = new Utf8JsonListReader<bool>();
            var output = reader.ReadFromJson(listString);

            Assert.IsTrue(list.SequenceEqual(output));
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
            var reader = new Utf8JsonListReader<List<int>>();
            var output = reader.ReadFromJson(listString);

            for (var i = 0; i < list.Count; i++)
            {
                Assert.IsTrue(list[i].SequenceEqual(output[i]));
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
            var reader = new Utf8JsonListReader<MultiPropertyTestObject>();
            var output = reader.ReadFromJson(listString);

            Assert.IsTrue(list.SequenceEqual(output));
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
                    .WithType(typeof(ObjectWrapperEntity<IEntity>))
            );

            var list = new List<IEntity>()
            {
                new ObjectWrapperEntity<IEntity>(context.TopRegistry[0]),
                null,
                new ObjectWrapperEntity<IEntity>(context.TopRegistry[1]),
                null,
                new ObjectWrapperEntity<IEntity>(context.TopRegistry[2])
            };

            var listString = WriteObjectToJsonString(list, EntitySerialization.AsValue);
            var reader = new Utf8JsonListReader<IEntity>();
            var output = reader.ReadFromJson(listString, context, EntitySerialization.AsValue);

            Assert.IsTrue(list.SequenceEqual(output));
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
                ClassLookup.CreateDefault().WithType(typeof(ObjectEntity<IEntity>))
            );

            var list = new List<IEntity>()
            {
                new ObjectEntity<IEntity>(context.TopRegistry[0]),
                new ObjectEntity<IEntity>(context.TopRegistry[1]),
                new ObjectEntity<IEntity>(context.TopRegistry[2]),
            };

            var listString = WriteObjectToJsonString(list, EntitySerialization.AsValue);
            var reader = new Utf8JsonListReader<IEntity>();
            var output = reader.ReadFromJson(listString, context, EntitySerialization.AsValue);

            Assert.IsTrue(list.SequenceEqual(output));
            foreach (var ent in list)
            {
                var count = output.Count(e => ((ObjectEntity<IEntity>)e).Value == ((ObjectEntity<IEntity>)ent).Value);
                Assert.IsTrue(count == 1);
            }
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

            var context = new JsonReaderContext(ClassLookup.CreateDefault().WithType(typeof(ObjectWrapperEntity<IEntity>)));

            var list = new List<List<IEntity>>()
            {
                new List<IEntity>()
                {
                    new ObjectWrapperEntity<IEntity>(registry[0]),
                    new ObjectWrapperEntity<IEntity>(registry[1]),
                },
                new List<IEntity>()
                {
                    new ObjectWrapperEntity<IEntity>(registry[0]),
                    new ObjectWrapperEntity<IEntity>(registry[2]),
                },
                new List<IEntity>()
                {
                    new ObjectWrapperEntity<IEntity>(registry[1]),
                    new ObjectWrapperEntity<IEntity>(registry[2]),
                },
            };

            var listString = WriteObjectToJsonString(list, EntitySerialization.AsValue);
            var reader = new Utf8JsonListReader<List<IEntity>>();
            var output = reader.ReadFromJson(listString, context, EntitySerialization.AsValue);

            for (var i = 0; i < list.Count; i++)
            {
                Assert.IsTrue(list[i].SequenceEqual(output[i]));
            }
        }
    }
}
