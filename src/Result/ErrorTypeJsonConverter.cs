using System.Text.Json;
using System.Text.Json.Serialization;

namespace Davish.Result;

/// <summary>
/// Serializes an <see cref="ErrorType"/> as its flat <see cref="ErrorType.Value"/> string, instead of as a
/// nested object.
/// </summary>
public sealed class ErrorTypeJsonConverter : JsonConverter<ErrorType>
{
    /// <inheritdoc/>
    public override ErrorType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(reader.GetString() ?? throw new JsonException("Error type value cannot be null."));

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, ErrorType value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}
