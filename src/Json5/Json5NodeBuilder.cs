using System.Text.Json.Nodes;

namespace Json5;

/// <summary>
/// Builds a <see cref="JsonNode"/> tree directly from JSON5 tokens.
/// </summary>
static class Json5NodeBuilder
{
    public static JsonNode? Build(ReadOnlySpan<byte> json5, Json5ReaderOptions options, JsonNodeOptions nodeOptions)
    {
        var tokenizer = new Json5Tokenizer(json5, options);

        if (!tokenizer.Read())
            throw new Json5Exception("Empty JSON5 input.", 0, 1, 1);

        var result = BuildValue(ref tokenizer, options, nodeOptions);

        // Ensure no trailing non-whitespace content
        tokenizer.SkipTrailingContent();

        return result;
    }

    static JsonNode? BuildValue(ref Json5Tokenizer tokenizer, Json5ReaderOptions options, JsonNodeOptions nodeOptions)
    {
        switch (tokenizer.TokenType)
        {
            case Json5TokenType.StartObject:
                return BuildObject(ref tokenizer, options, nodeOptions);

            case Json5TokenType.StartArray:
                return BuildArray(ref tokenizer, options, nodeOptions);

            case Json5TokenType.String:
                return JsonValue.Create(tokenizer.StringValue!, nodeOptions);

            case Json5TokenType.Number:
                if (tokenizer.NumberIsInteger)
                    return JsonValue.Create(tokenizer.IntegerValue, nodeOptions);
                return JsonValue.Create(tokenizer.NumberValue, nodeOptions);

            case Json5TokenType.True:
                return JsonValue.Create(true, nodeOptions);

            case Json5TokenType.False:
                return JsonValue.Create(false, nodeOptions);

            case Json5TokenType.Null:
                return null;

            case Json5TokenType.Infinity:
            case Json5TokenType.PositiveInfinity:
            case Json5TokenType.NegativeInfinity:
                return HandleSpecialNumber(tokenizer.NumberValue, tokenizer.TokenType, options, nodeOptions, ref tokenizer);

            case Json5TokenType.NaN:
                return HandleSpecialNumber(double.NaN, Json5TokenType.NaN, options, nodeOptions, ref tokenizer);

            default:
                throw new Json5Exception(
                    $"Unexpected token {tokenizer.TokenType} (line {tokenizer.Line}, col {tokenizer.Column})",
                    tokenizer.Position, tokenizer.Line, tokenizer.Column);
        }
    }

    static JsonObject BuildObject(ref Json5Tokenizer tokenizer, Json5ReaderOptions options, JsonNodeOptions nodeOptions)
    {
        var obj = new JsonObject(nodeOptions);

        while (tokenizer.ReadPropertyName())
        {
            var key = tokenizer.StringValue!;

            if (!tokenizer.Read())
                throw new Json5Exception(
                    $"Unexpected end of input in object (line {tokenizer.Line}, col {tokenizer.Column})",
                    tokenizer.Position, tokenizer.Line, tokenizer.Column);

            var value = BuildValue(ref tokenizer, options, nodeOptions);
            obj.Add(key, value);
        }

        // ReadPropertyName returned false → '}' was found, consume it
        tokenizer.ConsumeEndToken();

        return obj;
    }

    static JsonArray BuildArray(ref Json5Tokenizer tokenizer, Json5ReaderOptions options, JsonNodeOptions nodeOptions)
    {
        var arr = new JsonArray(nodeOptions);

        while (true)
        {
            if (!tokenizer.Read())
                throw new Json5Exception(
                    $"Unexpected end of input in array (line {tokenizer.Line}, col {tokenizer.Column})",
                    tokenizer.Position, tokenizer.Line, tokenizer.Column);

            if (tokenizer.TokenType == Json5TokenType.EndArray)
                return arr;

            var value = BuildValue(ref tokenizer, options, nodeOptions);
            arr.Add(value);
        }
    }

    static JsonNode? HandleSpecialNumber(double value, Json5TokenType tokenType, Json5ReaderOptions options, JsonNodeOptions nodeOptions, ref Json5Tokenizer tokenizer)
    {
        return options.SpecialNumbers switch
        {
            SpecialNumberHandling.AsString => JsonValue.Create(tokenType switch
            {
                Json5TokenType.NegativeInfinity => "-Infinity",
                Json5TokenType.NaN => "NaN",
                _ => "Infinity"
            }, nodeOptions),

            SpecialNumberHandling.AsNull => null,

            SpecialNumberHandling.Throw => throw new Json5Exception(
                $"Special number value '{tokenType}' is not allowed (line {tokenizer.Line}, col {tokenizer.Column})",
                tokenizer.Position, tokenizer.Line, tokenizer.Column),

            _ => throw new ArgumentOutOfRangeException(nameof(options))
        };
    }
}
