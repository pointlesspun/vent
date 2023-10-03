using System.Text.Json;

namespace Vent.ToJson.Readers
{
    public class Utf8JsonDateTimeReader : AbstractUtf8JsonReader<DateTime>
    {
        public override object ReadValue(ref Utf8JsonReader reader,
            JsonReaderContext _,
            EntitySerialization __)
        {
            return Utf8JsonDateTimeReaderExtensions.ReadDateTime(ref reader);
        }
    }

    public static class Utf8JsonDateTimeReaderExtensions
    {

        /// <summary>
        /// Read the date time from the current token in the reader. If the token
        /// is a number, it will be assumed to represent ticks and the date time
        /// returned will be based on these ticks. Otherwise DateTime.Parse
        /// will be used.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static DateTime ReadDateTime(this ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                // parse the datetime as if it were ticks
                return new DateTime(reader.GetInt64());
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                return DateTime.Parse(reader.GetString());
            }

            throw new JsonException($"JsonReader can't covert {reader.TokenType} to a DateTime.");
        }
    }
}
