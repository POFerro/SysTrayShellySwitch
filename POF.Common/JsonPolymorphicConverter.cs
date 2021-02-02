using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace POF.Common
{
    public abstract class PolymorphicConverter<T, TDiscriminator> : JsonConverter<T>
        where T: class
    {
        protected readonly Dictionary<TDiscriminator, Type> TypeMapping;
        protected readonly string DiscriminatorPropertyName;

        public PolymorphicConverter(string discriminatorPropertyName, Dictionary<TDiscriminator, Type> typeMapping)
        {
            this.TypeMapping = typeMapping;
            this.DiscriminatorPropertyName = discriminatorPropertyName;
        }

        protected abstract TDiscriminator ReadDisciminatorValue(ref Utf8JsonReader reader);

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected start of object");
            }
            var typeReader = reader;
            Type deserializedType = null;
            while (typeReader.Read())
            {
                if (typeReader.TokenType == JsonTokenType.PropertyName && typeReader.ValueTextEquals(this.DiscriminatorPropertyName))
                {
                    if (!typeReader.Read())
                    {
                        throw new JsonException($"Unable to read.");
                    }
                    TDiscriminator typeName = ReadDisciminatorValue(ref typeReader);
                    if (typeName is null)
                    {
                        throw new JsonException($"Unable to read the type name.");
                    }
                    if (!this.TypeMapping.TryGetValue(typeName, out deserializedType))
                    {
                        throw new JsonException($"Invalid type in json : {typeName}");
                    }
                    break;
                }
                else if (typeReader.TokenType == JsonTokenType.StartArray || typeReader.TokenType == JsonTokenType.StartObject)
                {
                    typeReader.Skip();
                }
            }

            if (deserializedType == null)
            {
                throw new JsonException($"Key 'Type' not found.");
            }

            return (T)JsonSerializer.Deserialize(ref reader, deserializedType, options);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value != null)
                JsonSerializer.Serialize(writer, value, value.GetType(), options);
            else
                JsonSerializer.Serialize(writer, value, options);
        }
    }
}