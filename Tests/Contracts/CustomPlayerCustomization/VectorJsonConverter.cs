using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace CustomPlayerCustomization
{
    public class VectorJsonConverter : JsonConverter<Vector3>
    {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                float x = 0, y = 0, z = 0;
                var count = 0;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.Number)
                    {
                        switch (count++)
                        {
                            case 0: x = reader.GetSingle(); break;
                            case 1: y = reader.GetSingle(); break;
                            case 2: z = reader.GetSingle(); break;
                        }
                    }
                    else if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        break;
                    }
                }

                return new Vector3(x, y, z);
            }

            throw new JsonException("Invalid format for Vector3");
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.x);
            writer.WriteNumberValue(value.y);
            writer.WriteNumberValue(value.z);
            writer.WriteEndArray();
        }
    }
}
