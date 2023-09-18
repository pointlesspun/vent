using Newtonsoft.Json;
using System.Diagnostics;
using Vent;

namespace Vent.ToJson.Test
{
    public class StringEntity : PropertyEntity<string>
    {
        public StringEntity() { }

        public StringEntity(string value) : base(value) { }
    }


    [TestClass]
    public class StoreConverterTest
    {
    
        [TestMethod]
        public void WriteToJsonTest()
        {
            var store = new EntityStore();

            store.Commit(new StringEntity("foo"));
            store.Commit(new StringEntity("bar"));

            var converter = new StoreConverter().RegisterEntityClasses(AppDomain.CurrentDomain.GetAssemblies());
            
            string json = JsonConvert.SerializeObject(store, Formatting.Indented, converter);

            Debug.WriteLine(json);

            var storeCopy = JsonConvert.DeserializeObject<EntityStore>(json, converter);

        }
    }
}