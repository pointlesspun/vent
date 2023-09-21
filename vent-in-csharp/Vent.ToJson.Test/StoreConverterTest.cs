using Newtonsoft.Json;
using System.Diagnostics;
using Vent.PropertyEntities;

namespace Vent.ToJson.Test
{
    

    [TestClass]
    public class StoreConverterTest
    {
    
        [TestMethod]
        public void WriteToJsonTest()
        {
            var registry = new EntityRegistry();
            var store = new EntityHistory(registry);

            var ent1 = store.Commit(new StringEntity("foo-1"));
            var ent2 = store.Commit(new StringEntity("foo-2"));

            store.Commit(ent1.With("bar-1"));
            store.Commit(ent2.With("bar-2"));

            var converter = new StoreConverter(typeof(IEntity).Assembly, typeof(StringEntity).Assembly);
            
            string json = JsonConvert.SerializeObject(store, Formatting.Indented, converter);

            Debug.WriteLine(json);

            var storeCopy = JsonConvert.DeserializeObject<EntityHistory>(json, converter);

            Assert.IsTrue(store.CurrentMutation == storeCopy.CurrentMutation);
            Assert.IsTrue(store.MutationCount == storeCopy.MutationCount);
            Assert.IsTrue(store.DeleteOutOfScopeVersions == storeCopy.DeleteOutOfScopeVersions);
            Assert.IsTrue(registry.SlotCount == storeCopy.Registry.SlotCount);
            Assert.IsTrue(registry.EntitiesInScope == storeCopy.Registry.EntitiesInScope);
            Assert.IsTrue(store.MaxEntitySlots == storeCopy.MaxEntitySlots);

            var ent1_ = (StringEntity) storeCopy.Registry[ent1.Id];
            var ent2_ = (StringEntity) storeCopy.Registry[ent2.Id];

            Assert.IsTrue(ent1.Value == ent1_.Value);
            Assert.IsTrue(ent2.Value == ent2_.Value);
            Assert.IsTrue(ent1.Id == ent1_.Id);
            Assert.IsTrue(ent2.Id == ent2_.Id);
            Assert.IsTrue(ent1 != ent1_);
            Assert.IsTrue(ent2 != ent2_);

            store.ToTail();
            storeCopy.ToTail();

            Assert.IsTrue(ent1.Value == "foo-1");
            Assert.IsTrue(ent2.Value == "foo-2");
            Assert.IsTrue(ent1.Value == ent1_.Value);
            Assert.IsTrue(ent2.Value == ent2_.Value);

            store.ToHead();
            storeCopy.ToHead();

            Assert.IsTrue(ent1.Value == "bar-1");
            Assert.IsTrue(ent2.Value == "bar-2");
            Assert.IsTrue(ent1.Value == ent1_.Value);
            Assert.IsTrue(ent2.Value == ent2_.Value);

        }
    }
}
