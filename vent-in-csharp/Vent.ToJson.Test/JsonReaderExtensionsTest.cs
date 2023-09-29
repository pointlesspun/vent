﻿using System.Text;
using System.Text.Json;

namespace Vent.ToJson.Test
{
    [TestClass]
    public class JsonReaderExtensionsTest
    {
        [TestMethod]
        public void ReadStringValueTest()
        {          
            var fooReader = new Utf8JsonReader(Encoding.UTF8.GetBytes("\"foo\""));
            var fooOutput = fooReader.ReadVentValue<string>();

            Assert.AreEqual("foo", fooOutput);

            var emptyReader = new Utf8JsonReader(Encoding.UTF8.GetBytes("\"\""));
            var emptyOutput = emptyReader.ReadVentValue<string>();

            Assert.AreEqual("", emptyOutput);

            var nullReader = new Utf8JsonReader(Encoding.UTF8.GetBytes("null"));
            var nullOutput = nullReader.ReadVentValue<string>();

            Assert.AreEqual(null, nullOutput);
        }

        [TestMethod]
        public void ReadDateTimeTest()
        {
            var dateTimeNow = DateTime.Now;
            var dateString = $"\"{dateTimeNow}\"";
            var nowReader = new Utf8JsonReader(Encoding.UTF8.GetBytes(dateString));
            var nowOutput = nowReader.ReadVentValue<DateTime>();

            // do a approximate test
            Assert.AreEqual(nowOutput.Year, dateTimeNow.Year);
            Assert.AreEqual(nowOutput.Month, dateTimeNow.Month);
            Assert.AreEqual(nowOutput.Day, dateTimeNow.Day);
            Assert.AreEqual(nowOutput.Hour, dateTimeNow.Hour);
            Assert.AreEqual(nowOutput.Minute, dateTimeNow.Minute);
            Assert.AreEqual(nowOutput.Second, dateTimeNow.Second);

            var ticksString = $"{dateTimeNow.Ticks}";
            var ticksReader = new Utf8JsonReader(Encoding.UTF8.GetBytes(ticksString));
            var ticksOutput = ticksReader.ReadVentValue<DateTime>();

            Assert.AreEqual(ticksOutput, dateTimeNow);
        }
    }
}

