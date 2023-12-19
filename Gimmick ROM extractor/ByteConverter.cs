using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gimmick_ROM_extractor
{
    /// <summary>
    /// Allows parsing of bytes from strings in a JSON. Strings need to be in the following format: 0xX
    /// </summary>
    internal class ByteConverter : JsonConverter<byte>
    {
        public override void Write(Utf8JsonWriter writer, byte value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(String.Format("0x{0:X}", value));
        }

        public override byte Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return Convert.ToByte(reader.GetString().Substring(2), 16);
                case JsonTokenType.Number:
                    byte output;
                    if (!reader.TryGetByte(out output))
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
