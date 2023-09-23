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

        public override bool Equals(object obj)
        {
            return obj is StringEntity entity &&
                   Id == entity.Id &&
                   Value == entity.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Value);
        }


    }

}
