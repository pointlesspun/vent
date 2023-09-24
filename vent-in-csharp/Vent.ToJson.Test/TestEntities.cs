using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vent.PropertyEntities;

namespace Vent.ToJson.Test
{

    public class ObjectWrapper<T> : ObjectEntity<T> where T : class
    {
        [SerializeAsValue]
        public override T Value { get => base.Value; set => base.Value = value; }

        public ObjectWrapper() : base() { }

        public ObjectWrapper(T value) : base(value) { }
    }

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

    /// <summary>
    /// Entity containing a reference to another entity
    /// </summary>
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

    public class IntListEntity : EntityBase
    {
        public List<int> IntList
        {
            get;
            set;
        }

        public override object Clone()
        {
            var clone = (IntListEntity) base.Clone();

            clone.IntList = new List<int>(IntList);

            return clone;
        }

        public override bool Equals(object obj)
        {
            return obj is IntListEntity entity
                   && Id == entity.Id
                   && ((IntList == null && entity.IntList == null)
                    || IntList.SequenceEqual(entity.IntList));
        }
    }

    public class EntityListEntity : EntityBase
    {
        public List<IEntity> EntityList
        {
            get;
            set;
        }

        public override object Clone()
        {
            var clone = (EntityListEntity)base.Clone();

            clone.EntityList = new List<IEntity>(EntityList.Select(e => e.Clone()).Cast<IEntity>());

            return clone;
        }

        public override bool Equals(object obj)
        {
            return obj is EntityListEntity entity
                   && Id == entity.Id
                   && ((EntityList == null && entity.EntityList == null)
                    || EntityList.SequenceEqual(entity.EntityList));
        }
    }
}
