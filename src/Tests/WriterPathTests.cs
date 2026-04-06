using System.Text.Json;
using Json5;
using J5 = Json5.Json5;

namespace Tests;

public class WriterPathTests
{
    // ── ParseDocument ───────────────────────────────────

    [Fact]
    public void ParseDocumentSimpleObject()
    {
        using var doc = J5.ParseDocument("{a: 1, b: 'two'}");
        var root = doc.RootElement;
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.Equal(1, root.GetProperty("a").GetInt32());
        Assert.Equal("two", root.GetProperty("b").GetString());
    }

    [Fact]
    public void ParseDocumentArray()
    {
        using var doc = J5.ParseDocument("[1, 2, 3]");
        var root = doc.RootElement;
        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(3, root.GetArrayLength());
    }

    [Fact]
    public void ParseDocumentWithJson5Features()
    {
        var json5 = """
            {
                // comment
                unquoted: 'single-quoted',
                hex: 0xFF,
                trailing: true,
            }
            """;
        using var doc = J5.ParseDocument(json5);
        var root = doc.RootElement;
        Assert.Equal("single-quoted", root.GetProperty("unquoted").GetString());
        Assert.Equal(255, root.GetProperty("hex").GetInt32());
        Assert.True(root.GetProperty("trailing").GetBoolean());
    }

    // ── ToJson ──────────────────────────────────────────

    [Fact]
    public void ToJsonSimple()
    {
        var result = J5.ToJson("{a: 1}");
        // Should be valid JSON
        using var doc = JsonDocument.Parse(result);
        Assert.Equal(1, doc.RootElement.GetProperty("a").GetInt32());
    }

    [Fact]
    public void ToJsonPreservesValues()
    {
        var result = J5.ToJson("'hello'");
        Assert.Equal("\"hello\"", result);
    }

    [Fact]
    public void ToJsonConvertsHex()
    {
        var result = J5.ToJson("0xFF");
        Assert.Equal("255", result);
    }

    [Fact]
    public void ToJsonConvertsLeadingDot()
    {
        var result = J5.ToJson(".5");
        // Should be a valid JSON number
        using var doc = JsonDocument.Parse(result);
        Assert.Equal(0.5, doc.RootElement.GetDouble());
    }

    // ── ToUtf8Json ──────────────────────────────────────

    [Fact]
    public void ToUtf8JsonRoundTrip()
    {
        var json5 = "{foo: [1, 'two', true, null]}"u8;
        var utf8 = J5.ToUtf8Json(json5);
        using var doc = JsonDocument.Parse(utf8);
        var root = doc.RootElement;
        Assert.Equal(JsonValueKind.Array, root.GetProperty("foo").ValueKind);
    }

    // ── WriteTo ─────────────────────────────────────────

    [Fact]
    public void WriteToWriter()
    {
        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        J5.WriteTo("{key: 'value'}", writer, default);
        writer.Flush();

        using var doc = JsonDocument.Parse(buffer.WrittenSpan.ToArray());
        Assert.Equal("value", doc.RootElement.GetProperty("key").GetString());
    }

    [Fact]
    public void WriteToFromUtf8()
    {
        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        J5.WriteTo("{x: 42}"u8, writer, default);
        writer.Flush();

        using var doc = JsonDocument.Parse(buffer.WrittenSpan.ToArray());
        Assert.Equal(42, doc.RootElement.GetProperty("x").GetInt32());
    }
}
