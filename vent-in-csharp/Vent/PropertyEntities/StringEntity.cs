using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vent.PropertyEntities
{
    public class StringEntity : PropertyEntity<string>
    {
        public StringEntity() { }

        public StringEntity(string value) : base(value) { }
    }

}
