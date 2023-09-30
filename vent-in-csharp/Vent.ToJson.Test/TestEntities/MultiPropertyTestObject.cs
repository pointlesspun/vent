﻿namespace Vent.ToJson.Test.TestEntities
{
    /// <summary>
    /// Entity with multiple primitive properties for testing
    /// </summary>
    public class MultiPropertyTestObject 
    {
        public bool BooleanValue { get; set; }

        public string StringValue { get; set; }

        public char CharValue { get; set; }

        public int IntValue { get; set; }

        public uint UIntValue { get; set; }

        public float FloatValue { get; set; }

        public double DoubleValue { get; set; }

        public MultiPropertyTestObject()
        {
        }

        public MultiPropertyTestObject(bool booleanValue, string stringValue, char charValue, int intValue, uint uIntValue, float floatValue, double doubleValue)
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
                && obj is MultiPropertyTestObject other
                && other.BooleanValue == BooleanValue
                && other.StringValue == StringValue
                && other.CharValue == CharValue
                && other.UIntValue == UIntValue
                && Math.Abs(other.FloatValue - FloatValue) < 0.0001f
                && Math.Abs(other.DoubleValue - DoubleValue) < 0.0001;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }


}