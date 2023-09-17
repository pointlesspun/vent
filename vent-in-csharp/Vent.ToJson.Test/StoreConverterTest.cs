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

            string json = JsonConvert.SerializeObject(store, Formatting.Indented, new StoreConverter());

            Debug.WriteLine(json);

        }
    }
}