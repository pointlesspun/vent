using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vent.ToJson.Test
{
    /// <summary>
    /// Entity with multiple primitive properties for testing
    /// </summary>
    public class MultiPropertyTestEntity : EntityBase
    {
        public bool BooleanValue { get; set; }

        public string StringValue { get; set; }

        public char CharValue { get; set; }
        
        public int IntValue { get; set; }

        public uint UIntValue { get; set; }

        public float FloatValue { get; set; }

        public double DoubleValue { get; set; }

        public MultiPropertyTestEntity()
        {
        }

        public MultiPropertyTestEntity(bool booleanValue, string stringValue, char charValue, int intValue, uint uIntValue, float floatValue, double doubleValue)
        {
            BooleanValue = booleanValue;
            StringValue = stringValue;
            CharValue = charValue;
            IntValue = intValue;
            UIntValue = uIntValue;
            FloatValue = floatValue;
            DoubleValue = doubleValue;
        }

        public override bool Equals(object obj)
        {
            if (obj == this) return true;

            return obj != null
                && obj is MultiPropertyTestEntity other
                && other.Id == Id
                && other.BooleanValue == BooleanValue
                && other.StringValue == StringValue
                && other.CharValue == CharValue
                && other.UIntValue == UIntValue
                && Math.Abs(other.FloatValue - FloatValue) < 0.0001f
                && Math.Abs(other.DoubleValue - DoubleValue) < 0.0001;
        }
    }

    public class EntityPropertyEntity : EntityBase
    {
        public IEntity Other { get; set; }

        public EntityPropertyEntity() { }   

        public EntityPropertyEntity(IEntity other)
        {
            Other = other;
        }

        public override bool Equals(object obj)
        {
            if (obj == this) return true;

            return obj != null
                && obj is EntityPropertyEntity other
                && (other.Other == null && Other == null
                    || other.Other.Id == Other.Id);
        }
    }
}
