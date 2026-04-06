using System.Text.Json;
using System.Text.Json.Nodes;
using Json5;
using J5 = Json5.Json5;

namespace Tests;

public class ParseTests
{
    // ── Primitives ───────────────────────────────────────

    [Fact]
    public void ParseNull() => Assert.Null(J5.Parse("null"));

    [Fact]
    public void ParseTrue() => Assert.True(J5.Parse("true")!.GetValue<bool>());

    [Fact]
    public void ParseFalse() => Assert.False(J5.Parse("false")!.GetValue<bool>());

    [Theory]
    [InlineData("42", 42L)]
    [InlineData("-1", -1L)]
    [InlineData("0", 0L)]
    [InlineData("+1", 1L)]
    public void ParseInteger(string input, long expected)
        => Assert.Equal(expected, J5.Parse(input)!.GetValue<long>());

    [Theory]
    [InlineData("3.14", 3.14)]
    [InlineData("-0.5", -0.5)]
    [InlineData("1e10", 1e10)]
    [InlineData("1.5e2", 150.0)]
    public void ParseFloat(string input, double expected)
        => Assert.Equal(expected, J5.Parse(input)!.GetValue<double>());

    // ── Strings ─────────────────────────────────────────

    [Theory]
    [InlineData("\"hello\"", "hello")]
    [InlineData("'hello'", "hello")]
    [InlineData("\"with 'single' quotes\"", "with 'single' quotes")]
    [InlineData("'with \"double\" quotes'", "with \"double\" quotes")]
    public void ParseString(string input, string expected)
        => Assert.Equal(expected, J5.Parse(input)!.GetValue<string>());

    [Theory]
    [InlineData("\"\\n\"", "\n")]
    [InlineData("\"\\t\"", "\t")]
    [InlineData("\"\\r\"", "\r")]
    [InlineData("\"\\b\"", "\b")]
    [InlineData("\"\\f\"", "\f")]
    [InlineData("\"\\v\"", "\v")]
    [InlineData("\"\\0\"", "\0")]
    [InlineData("\"\\\\\"", "\\")]
    [InlineData("\"\\/\"", "/")]
    public void ParseStringEscapes(string input, string expected)
        => Assert.Equal(expected, J5.Parse(input)!.GetValue<string>());

    [Fact]
    public void ParseHexEscape()
        => Assert.Equal("A", J5.Parse("\"\\x41\"")!.GetValue<string>());

    [Fact]
    public void ParseUnicodeEscape()
        => Assert.Equal("\u00E9", J5.Parse("\"\\u00e9\"")!.GetValue<string>());

    [Fact]
    public void ParseMultiLineString()
    {
        // Backslash followed by newline is a line continuation (removed)
        var input = "'line1\\\nline2'";
        Assert.Equal("line1line2", J5.Parse(input)!.GetValue<string>());
    }

    [Fact]
    public void ParseIdentityEscape()
    {
        // \a is an identity escape — the 'a' character itself
        Assert.Equal("a", J5.Parse("\"\\a\"")!.GetValue<string>());
    }

    // ── JSON5 Numbers ───────────────────────────────────

    [Fact]
    public void ParseHexNumber()
        => Assert.Equal(255L, J5.Parse("0xFF")!.GetValue<long>());

    [Fact]
    public void ParseHexUpperCase()
        => Assert.Equal(0xDECAF, J5.Parse("0xDECAF")!.GetValue<long>());

    [Fact]
    public void ParseNegativeHex()
        => Assert.Equal(-255L, J5.Parse("-0xFF")!.GetValue<long>());

    [Fact]
    public void ParseLeadingDecimalPoint()
        => Assert.Equal(0.5, J5.Parse(".5")!.GetValue<double>());

    [Fact]
    public void ParseTrailingDecimalPoint()
        => Assert.Equal(2.0, J5.Parse("2.")!.GetValue<double>());

    [Fact]
    public void ParsePositiveSign()
        => Assert.Equal(42L, J5.Parse("+42")!.GetValue<long>());

    // ── Infinity / NaN ──────────────────────────────────

    [Fact]
    public void ParseInfinityAsString()
    {
        var node = J5.Parse("Infinity");
        Assert.Equal("Infinity", node!.GetValue<string>());
    }

    [Fact]
    public void ParseNegativeInfinityAsString()
    {
        var node = J5.Parse("-Infinity");
        Assert.Equal("-Infinity", node!.GetValue<string>());
    }

    [Fact]
    public void ParseNaNAsString()
    {
        var node = J5.Parse("NaN");
        Assert.Equal("NaN", node!.GetValue<string>());
    }

    [Fact]
    public void ParseInfinityAsNull()
    {
        var options = new Json5ReaderOptions { SpecialNumbers = SpecialNumberHandling.AsNull };
        Assert.Null(J5.Parse("Infinity", options));
    }

    [Fact]
    public void ParseInfinityThrows()
    {
        var options = new Json5ReaderOptions { SpecialNumbers = SpecialNumberHandling.Throw };
        Assert.Throws<Json5Exception>(() => J5.Parse("Infinity", options));
    }

    // ── Objects ──────────────────────────────────────────

    [Fact]
    public void ParseEmptyObject()
    {
        var node = J5.Parse("{}");
        Assert.IsType<JsonObject>(node);
        Assert.Empty(node!.AsObject());
    }

    [Fact]
    public void ParseSimpleObject()
    {
        var node = J5.Parse("{\"a\": 1, \"b\": 2}");
        Assert.Equal(1L, node!["a"]!.GetValue<long>());
        Assert.Equal(2L, node!["b"]!.GetValue<long>());
    }

    [Fact]
    public void ParseUnquotedKeys()
    {
        var node = J5.Parse("{foo: 1, bar: 'hello'}");
        Assert.Equal(1L, node!["foo"]!.GetValue<long>());
        Assert.Equal("hello", node!["bar"]!.GetValue<string>());
    }

    [Fact]
    public void ParseSingleQuotedKeys()
    {
        var node = J5.Parse("{'key': 'value'}");
        Assert.Equal("value", node!["key"]!.GetValue<string>());
    }

    [Fact]
    public void ParseTrailingCommaObject()
    {
        var node = J5.Parse("{a: 1, b: 2,}");
        Assert.Equal(1L, node!["a"]!.GetValue<long>());
        Assert.Equal(2L, node!["b"]!.GetValue<long>());
    }

    [Fact]
    public void ParseNestedObject()
    {
        var node = J5.Parse("{outer: {inner: 42}}");
        Assert.Equal(42L, node!["outer"]!["inner"]!.GetValue<long>());
    }

    // ── Arrays ──────────────────────────────────────────

    [Fact]
    public void ParseEmptyArray()
    {
        var node = J5.Parse("[]");
        Assert.IsType<JsonArray>(node);
        Assert.Empty(node!.AsArray());
    }

    [Fact]
    public void ParseSimpleArray()
    {
        var node = J5.Parse("[1, 2, 3]");
        var arr = node!.AsArray();
        Assert.Equal(3, arr.Count);
        Assert.Equal(1L, arr[0]!.GetValue<long>());
        Assert.Equal(2L, arr[1]!.GetValue<long>());
        Assert.Equal(3L, arr[2]!.GetValue<long>());
    }

    [Fact]
    public void ParseTrailingCommaArray()
    {
        var node = J5.Parse("[1, 2, 3,]");
        Assert.Equal(3, node!.AsArray().Count);
    }

    [Fact]
    public void ParseMixedArray()
    {
        var node = J5.Parse("[1, 'two', true, null, {a: 3}]");
        var arr = node!.AsArray();
        Assert.Equal(5, arr.Count);
        Assert.Equal(1L, arr[0]!.GetValue<long>());
        Assert.Equal("two", arr[1]!.GetValue<string>());
        Assert.True(arr[2]!.GetValue<bool>());
        Assert.Null(arr[3]);
        Assert.Equal(3L, arr[4]!["a"]!.GetValue<long>());
    }

    // ── Comments ────────────────────────────────────────

    [Fact]
    public void ParseSingleLineComment()
    {
        var node = J5.Parse("// comment\n42");
        Assert.Equal(42L, node!.GetValue<long>());
    }

    [Fact]
    public void ParseMultiLineComment()
    {
        var node = J5.Parse("/* comment */ 42");
        Assert.Equal(42L, node!.GetValue<long>());
    }

    [Fact]
    public void ParseCommentsInObject()
    {
        var input = """
            {
                // name
                name: 'test',
                /* version */
                version: 1
            }
            """;
        var node = J5.Parse(input);
        Assert.Equal("test", node!["name"]!.GetValue<string>());
        Assert.Equal(1L, node!["version"]!.GetValue<long>());
    }

    // ── Whitespace ──────────────────────────────────────

    [Fact]
    public void ParseLeadingTrailingWhitespace()
    {
        var node = J5.Parse("  \t\n 42 \n\t ");
        Assert.Equal(42L, node!.GetValue<long>());
    }

    [Fact]
    public void ParseBOM()
    {
        // UTF-8 BOM: EF BB BF
        var input = "\uFEFF42";
        var node = J5.Parse(input);
        Assert.Equal(42L, node!.GetValue<long>());
    }

    // ── Error cases ─────────────────────────────────────

    [Fact]
    public void ParseEmptyInputThrows()
        => Assert.Throws<Json5Exception>(() => J5.Parse(""));

    [Fact]
    public void ParseTrailingContentThrows()
        => Assert.Throws<Json5Exception>(() => J5.Parse("42 43"));

    [Fact]
    public void ParseUnterminatedStringThrows()
        => Assert.Throws<Json5Exception>(() => J5.Parse("\"unterminated"));

    [Fact]
    public void ParseUnterminatedObjectThrows()
        => Assert.Throws<Json5Exception>(() => J5.Parse("{a: 1"));

    [Fact]
    public void ParseUnterminatedArrayThrows()
        => Assert.Throws<Json5Exception>(() => J5.Parse("[1, 2"));

    [Fact]
    public void ParseInvalidEscapeDigitThrows()
        => Assert.Throws<Json5Exception>(() => J5.Parse("\"\\1\""));
}
