using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vent.ToJson
{
    public class JsonReaderContext
    {
        public Dictionary<string, Type> ClassLookup { get; set; }

        public EntityRegistry Registry { get; set; }

        public Dictionary<object, List<ForwardReference>> ForwardReferenceLookup { get; set; }  
            = new Dictionary<object, List<ForwardReference>>();
        public JsonReaderContext() { }

        public List<ForwardReference> AddReferenceList(object target)
        {
            var list = new List<ForwardReference>();
            ForwardReferenceLookup.Add(target, list);
            return list;
        }
    }
}
