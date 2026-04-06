using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Json5;

/// <summary>
/// Provides methods for parsing JSON5 text into System.Text.Json types.
/// </summary>
public static class Json5
{
    // ── JsonNode path (via Json5NodeBuilder) ──────────────────────────

    /// <summary>
    /// Parses a JSON5 string into a <see cref="JsonNode"/>.
    /// </summary>
    public static JsonNode? Parse(string json5, Json5ReaderOptions options = default, JsonNodeOptions? nodeOptions = null)
    {
        ThrowIfNull(json5);
        var utf8 = Encoding.UTF8.GetBytes(json5);
        return Parse(utf8.AsSpan(), options, nodeOptions);
    }

    /// <summary>
    /// Parses JSON5 UTF-8 bytes into a <see cref="JsonNode"/>.
    /// </summary>
    public static JsonNode? Parse(ReadOnlySpan<byte> json5, Json5ReaderOptions options = default, JsonNodeOptions? nodeOptions = null)
        => Json5NodeBuilder.Build(json5, options, nodeOptions ?? new JsonNodeOptions { PropertyNameCaseInsensitive = false });

    // ── Utf8JsonWriter path (via Json5Writer) ────────────────────────

    /// <summary>
    /// Deserializes a JSON5 string to an instance of <typeparamref name="T"/>.
    /// </summary>
    public static T? Deserialize<T>(string json5, JsonSerializerOptions? options = null, Json5ReaderOptions json5Options = default)
    {
        ThrowIfNull(json5);
        var utf8 = Encoding.UTF8.GetBytes(json5);
        return Deserialize<T>(utf8.AsSpan(), options, json5Options);
    }

    /// <summary>
    /// Deserializes JSON5 UTF-8 bytes to an instance of <typeparamref name="T"/>.
    /// </summary>
    public static T? Deserialize<T>(ReadOnlySpan<byte> json5, JsonSerializerOptions? options = null, Json5ReaderOptions json5Options = default)
    {
        var buffer = TranscodeToBuffer(json5, json5Options);
        var reader = new Utf8JsonReader(buffer, new JsonReaderOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });
        return JsonSerializer.Deserialize<T>(ref reader, options);
    }

    /// <summary>
    /// Deserializes a JSON5 string to the specified type.
    /// </summary>
    public static object? Deserialize(string json5, Type type, JsonSerializerOptions? options = null, Json5ReaderOptions json5Options = default)
    {
        ThrowIfNull(json5);
        var utf8 = Encoding.UTF8.GetBytes(json5);
        return Deserialize(utf8.AsSpan(), type, options, json5Options);
    }

    /// <summary>
    /// Deserializes JSON5 UTF-8 bytes to the specified type.
    /// </summary>
    public static object? Deserialize(ReadOnlySpan<byte> json5, Type type, JsonSerializerOptions? options = null, Json5ReaderOptions json5Options = default)
    {
        var buffer = TranscodeToBuffer(json5, json5Options);
        var reader = new Utf8JsonReader(buffer, new JsonReaderOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });
        return JsonSerializer.Deserialize(ref reader, type, options);
    }

    /// <summary>
    /// Parses a JSON5 string into a read-only <see cref="JsonDocument"/>.
    /// </summary>
    public static JsonDocument ParseDocument(string json5, Json5ReaderOptions json5Options = default, JsonDocumentOptions docOptions = default)
    {
        ThrowIfNull(json5);
        var utf8 = Encoding.UTF8.GetBytes(json5);
        return ParseDocument(utf8.AsSpan(), json5Options, docOptions);
    }

    /// <summary>
    /// Parses JSON5 UTF-8 bytes into a read-only <see cref="JsonDocument"/>.
    /// </summary>
    public static JsonDocument ParseDocument(ReadOnlySpan<byte> json5, Json5ReaderOptions json5Options = default, JsonDocumentOptions docOptions = default)
    {
        var buffer = TranscodeToBuffer(json5, json5Options);
        return JsonDocument.Parse(buffer.AsMemory(), docOptions);
    }

    /// <summary>
    /// Converts a JSON5 string to a standard JSON string.
    /// </summary>
    public static string ToJson(string json5, Json5ReaderOptions json5Options = default, JsonWriterOptions? writerOptions = null)
    {
        ThrowIfNull(json5);
        var utf8 = Encoding.UTF8.GetBytes(json5);
        var buffer = TranscodeToBuffer(utf8, json5Options, writerOptions);
        return Encoding.UTF8.GetString(buffer);
    }

    /// <summary>
    /// Converts JSON5 UTF-8 bytes to standard JSON UTF-8 bytes.
    /// </summary>
    public static byte[] ToUtf8Json(ReadOnlySpan<byte> json5, Json5ReaderOptions json5Options = default, JsonWriterOptions? writerOptions = null)
        => TranscodeToBuffer(json5, json5Options, writerOptions);

    /// <summary>
    /// Writes JSON5 input to the specified <see cref="Utf8JsonWriter"/>.
    /// </summary>
    public static void WriteTo(ReadOnlySpan<byte> json5, Utf8JsonWriter writer, Json5ReaderOptions options = default)
        => Json5Writer.WriteTo(json5, writer, options);

    /// <summary>
    /// Writes JSON5 input to the specified <see cref="Utf8JsonWriter"/>.
    /// </summary>
    public static void WriteTo(string json5, Utf8JsonWriter writer, Json5ReaderOptions options = default)
    {
        ThrowIfNull(json5);
        var utf8 = Encoding.UTF8.GetBytes(json5);
        Json5Writer.WriteTo(utf8, writer, options);
    }

    // ── Private helpers ──────────────────────────────────────────────

    static byte[] TranscodeToBuffer(ReadOnlySpan<byte> json5, Json5ReaderOptions json5Options, JsonWriterOptions? writerOptions = null)
    {
        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, writerOptions ?? new JsonWriterOptions { Indented = false, SkipValidation = true });
        Json5Writer.WriteTo(json5, writer, json5Options);
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }
}
