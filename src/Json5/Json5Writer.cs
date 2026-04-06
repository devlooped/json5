using System.Text.Json;

namespace Json5;

/// <summary>
/// Writes JSON5 tokens to a <see cref="Utf8JsonWriter"/> as standard JSON.
/// </summary>
static class Json5Writer
{
    public static void WriteTo(ReadOnlySpan<byte> json5, Utf8JsonWriter writer, Json5ReaderOptions options)
    {
        var tokenizer = new Json5Tokenizer(json5, options);

        if (!tokenizer.Read())
            throw new Json5Exception("Empty JSON5 input.", 0, 1, 1);

        WriteValue(ref tokenizer, writer, options);
        writer.Flush();
    }

    static void WriteValue(ref Json5Tokenizer tokenizer, Utf8JsonWriter writer, Json5ReaderOptions options)
    {
        switch (tokenizer.TokenType)
        {
            case Json5TokenType.StartObject:
                WriteObject(ref tokenizer, writer, options);
                break;

            case Json5TokenType.StartArray:
                WriteArray(ref tokenizer, writer, options);
                break;

            case Json5TokenType.String:
                writer.WriteStringValue(tokenizer.StringValue);
                break;

            case Json5TokenType.Number:
                if (tokenizer.NumberIsInteger)
                    writer.WriteNumberValue(tokenizer.IntegerValue);
                else
                    writer.WriteNumberValue(tokenizer.NumberValue);
                break;

            case Json5TokenType.True:
                writer.WriteBooleanValue(true);
                break;

            case Json5TokenType.False:
                writer.WriteBooleanValue(false);
                break;

            case Json5TokenType.Null:
                writer.WriteNullValue();
                break;

            case Json5TokenType.Infinity:
            case Json5TokenType.PositiveInfinity:
            case Json5TokenType.NegativeInfinity:
                WriteSpecialNumber(tokenizer.TokenType, writer, options, ref tokenizer);
                break;

            case Json5TokenType.NaN:
                WriteSpecialNumber(Json5TokenType.NaN, writer, options, ref tokenizer);
                break;

            default:
                throw new Json5Exception(
                    $"Unexpected token {tokenizer.TokenType} (line {tokenizer.Line}, col {tokenizer.Column})",
                    tokenizer.Position, tokenizer.Line, tokenizer.Column);
        }
    }

    static void WriteObject(ref Json5Tokenizer tokenizer, Utf8JsonWriter writer, Json5ReaderOptions options)
    {
        writer.WriteStartObject();

        while (tokenizer.ReadPropertyName())
        {
            writer.WritePropertyName(tokenizer.StringValue!);

            if (!tokenizer.Read())
                throw new Json5Exception(
                    $"Unexpected end of input in object (line {tokenizer.Line}, col {tokenizer.Column})",
                    tokenizer.Position, tokenizer.Line, tokenizer.Column);

            WriteValue(ref tokenizer, writer, options);
        }

        tokenizer.ConsumeEndToken();
        writer.WriteEndObject();
    }

    static void WriteArray(ref Json5Tokenizer tokenizer, Utf8JsonWriter writer, Json5ReaderOptions options)
    {
        writer.WriteStartArray();

        while (true)
        {
            if (!tokenizer.Read())
                throw new Json5Exception(
                    $"Unexpected end of input in array (line {tokenizer.Line}, col {tokenizer.Column})",
                    tokenizer.Position, tokenizer.Line, tokenizer.Column);

            if (tokenizer.TokenType == Json5TokenType.EndArray)
            {
                writer.WriteEndArray();
                return;
            }

            WriteValue(ref tokenizer, writer, options);
        }
    }

    static void WriteSpecialNumber(Json5TokenType tokenType, Utf8JsonWriter writer, Json5ReaderOptions options, ref Json5Tokenizer tokenizer)
    {
        switch (options.SpecialNumbers)
        {
            case SpecialNumberHandling.AsString:
                writer.WriteStringValue(tokenType switch
                {
                    Json5TokenType.NegativeInfinity => "-Infinity",
                    Json5TokenType.NaN => "NaN",
                    _ => "Infinity"
                });
                break;

            case SpecialNumberHandling.AsNull:
                writer.WriteNullValue();
                break;

            case SpecialNumberHandling.Throw:
                throw new Json5Exception(
                    $"Special number value '{tokenType}' is not allowed (line {tokenizer.Line}, col {tokenizer.Column})",
                    tokenizer.Position, tokenizer.Line, tokenizer.Column);
        }
    }
}
