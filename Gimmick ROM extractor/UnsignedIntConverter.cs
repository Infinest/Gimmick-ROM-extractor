using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gimmick_ROM_extractor
{
    /// <summary>
    /// Allows parsing of uints from strings in a JSON. Strings need to be in the following format: 0xX
    /// </summary>
    internal class UnsignedIntConverter: JsonConverter<uint>
    {
        public override void Write(Utf8JsonWriter writer, uint value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(String.Format("0x{0:X}", value));
        }

        public override uint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return Convert.ToUInt32(reader.GetString().Substring(2), 16);
                case JsonTokenType.Number:
                    uint output;
                    if (!reader.TryGetUInt32(out output))
                    {
                        throw new JsonException();
                    }
                    return output;
                default:
                    throw new JsonException();
            }
        }
    }
}
