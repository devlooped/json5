using System.Text;
using Json5;
using Microsoft.Extensions.Configuration;

namespace Tests;

public class Json5ConfigurationTests
{
    [Fact]
    public void ParsesSimpleKeyValue()
    {
        var json5 = "{ key: 'value' }";
        var config = LoadFromJson5(json5);

        Assert.Equal("value", config["key"]);
    }

    [Fact]
    public void ParsesNestedObjects()
    {
        var json5 = """
        {
            section: {
                key: 'value',
                nested: {
                    deep: 42,
                }
            }
        }
        """;
        var config = LoadFromJson5(json5);

        Assert.Equal("value", config["section:key"]);
        Assert.Equal("42", config["section:nested:deep"]);
    }

    [Fact]
    public void ParsesArrays()
    {
        var json5 = """
        {
            items: ['a', 'b', 'c']
        }
        """;
        var config = LoadFromJson5(json5);

        Assert.Equal("a", config["items:0"]);
        Assert.Equal("b", config["items:1"]);
        Assert.Equal("c", config["items:2"]);
    }

    [Fact]
    public void ParsesArrayOfObjects()
    {
        var json5 = """
        {
            people: [
                { name: 'Alice', age: 30 },
                { name: 'Bob', age: 25 },
            ]
        }
        """;
        var config = LoadFromJson5(json5);

        Assert.Equal("Alice", config["people:0:name"]);
        Assert.Equal("30", config["people:0:age"]);
        Assert.Equal("Bob", config["people:1:name"]);
        Assert.Equal("25", config["people:1:age"]);
    }

    [Fact]
    public void ParsesBooleans()
    {
        var json5 = "{ enabled: true, disabled: false }";
        var config = LoadFromJson5(json5);

        Assert.Equal("true", config["enabled"]);
        Assert.Equal("false", config["disabled"]);
    }

    [Fact]
    public void ParsesNullValues()
    {
        var json5 = "{ key: null }";
        var config = LoadFromJson5(json5);

        Assert.Null(config["key"]);
    }

    [Fact]
    public void ParsesSingleLineComments()
    {
        var json5 = """
        {
            // This is a comment
            key: 'value'
        }
        """;
        var config = LoadFromJson5(json5);

        Assert.Equal("value", config["key"]);
    }

    [Fact]
    public void ParsesMultiLineComments()
    {
        var json5 = """
        {
            /* multi-line
               comment */
            key: 'value'
        }
        """;
        var config = LoadFromJson5(json5);

        Assert.Equal("value", config["key"]);
    }

    [Fact]
    public void ParsesTrailingCommas()
    {
        var json5 = """
        {
            a: 1,
            b: 2,
        }
        """;
        var config = LoadFromJson5(json5);

        Assert.Equal("1", config["a"]);
        Assert.Equal("2", config["b"]);
    }

    [Fact]
    public void ParsesUnquotedKeys()
    {
        var json5 = "{ unquoted: 'works' }";
        var config = LoadFromJson5(json5);

        Assert.Equal("works", config["unquoted"]);
    }

    [Fact]
    public void ParsesSingleQuotedStrings()
    {
        var json5 = "{ key: 'single quoted' }";
        var config = LoadFromJson5(json5);

        Assert.Equal("single quoted", config["key"]);
    }

    [Fact]
    public void ParsesHexNumbers()
    {
        var json5 = "{ hex: 0xFF }";
        var config = LoadFromJson5(json5);

        Assert.Equal("255", config["hex"]);
    }

    [Fact]
    public void ParsesMultilineStrings()
    {
        var json5 = "{ key: 'line1\\\nline2' }";
        var config = LoadFromJson5(json5);

        Assert.Equal("line1line2", config["key"]);
    }

    [Fact]
    public void ThrowsForNonObjectRoot()
    {
        var json5 = "['a', 'b']";

        Assert.Throws<FormatException>(() => LoadFromJson5(json5));
    }

    [Fact]
    public void KeysAreCaseInsensitive()
    {
        var json5 = "{ Key: 'value' }";
        var config = LoadFromJson5(json5);

        Assert.Equal("value", config["key"]);
        Assert.Equal("value", config["KEY"]);
    }

    [Fact]
    public void ParsesInfinity()
    {
        var json5 = "{ val: Infinity }";
        var config = LoadFromJson5(json5);

        Assert.NotNull(config["val"]);
    }

    [Fact]
    public void ParsesNaN()
    {
        var json5 = "{ val: NaN }";
        var config = LoadFromJson5(json5);

        Assert.NotNull(config["val"]);
    }

    [Fact]
    public void ParsesNegativeInfinity()
    {
        var json5 = "{ val: -Infinity }";
        var config = LoadFromJson5(json5);

        Assert.NotNull(config["val"]);
    }

    [Fact]
    public void StreamProviderLoadsData()
    {
        var json5 = "{ key: 'value' }";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json5));

        var config = new ConfigurationBuilder()
            .AddJson5Stream(stream)
            .Build();

        Assert.Equal("value", config["key"]);
    }

    [Fact]
    public void SupportsReaderOptions()
    {
        var json5 = "{ val: Infinity }";

        var config = new ConfigurationBuilder()
            .AddJson5File(s =>
            {
                s.FileProvider = new InMemoryFileProvider(json5);
                s.Path = "test.json5";
                s.Json5ReaderOptions = new Json5ReaderOptions { SpecialNumbers = SpecialNumberHandling.AsNull };
            })
            .Build();

        Assert.Null(config["val"]);
    }

    [Fact]
    public void AddJson5FileThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IConfigurationBuilder)null!).AddJson5File("test.json5"));
    }

    [Fact]
    public void AddJson5FileThrowsOnEmptyPath()
    {
        var builder = new ConfigurationBuilder();

        Assert.Throws<ArgumentException>(() =>
            builder.AddJson5File(string.Empty));
    }

    [Fact]
    public void EmptyObjectProducesNull()
    {
        var json5 = "{ section: {} }";
        var config = LoadFromJson5(json5);

        Assert.Null(config["section"]);
    }

    [Fact]
    public void EmptyArrayProducesEmpty()
    {
        var json5 = "{ items: [] }";
        var config = LoadFromJson5(json5);

        Assert.Equal(string.Empty, config["items"]);
    }

    [Fact]
    public void AddJson5File_WithPathAndOptions_UsesReaderOptions()
    {
        var json5 = "{ val: Infinity }";

        var config = new ConfigurationBuilder()
            .SetFileProvider(new InMemoryFileProvider(json5))
            .AddJson5File("test.json5", options: new Json5ReaderOptions { SpecialNumbers = SpecialNumberHandling.AsNull })
            .Build();

        Assert.Null(config["val"]);
    }

    [Fact]
    public void AddJson5File_WithPathAndOptions_DefaultOptionsProduceString()
    {
        var json5 = "{ val: Infinity }";

        var config = new ConfigurationBuilder()
            .SetFileProvider(new InMemoryFileProvider(json5))
            .AddJson5File("test.json5")
            .Build();

        Assert.NotNull(config["val"]);
    }

    [Fact]
    public void AddJson5File_WithProviderPathAndOptions_UsesReaderOptions()
    {
        var json5 = "{ val: NaN }";

        var config = new ConfigurationBuilder()
            .AddJson5File(new InMemoryFileProvider(json5), "test.json5", options: new Json5ReaderOptions { SpecialNumbers = SpecialNumberHandling.AsNull })
            .Build();

        Assert.Null(config["val"]);
    }

    [Fact]
    public void AddJson5File_WithProviderPathAndOptions_DefaultOptionsProduceString()
    {
        var json5 = "{ val: NaN }";

        var config = new ConfigurationBuilder()
            .AddJson5File(new InMemoryFileProvider(json5), "test.json5")
            .Build();

        Assert.NotNull(config["val"]);
    }

    [Fact]
    public void AddJson5Stream_WithOptions_UsesReaderOptions()
    {
        var json5 = "{ val: -Infinity }";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json5));

        var config = new ConfigurationBuilder()
            .AddJson5Stream(stream, new Json5ReaderOptions { SpecialNumbers = SpecialNumberHandling.AsNull })
            .Build();

        Assert.Null(config["val"]);
    }

    [Fact]
    public void AddJson5Stream_WithOptions_DefaultOptionsProduceString()
    {
        var json5 = "{ val: -Infinity }";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json5));

        var config = new ConfigurationBuilder()
            .AddJson5Stream(stream)
            .Build();

        Assert.NotNull(config["val"]);
    }

    [Fact]
    public void AddJson5File_WithProviderPathAndAutoDedentOption_DedentsMultilineString()
    {
        // '\n    line1\n    line2\n' resolves to a string with newlines and common 4-space indent
        var json5 = "{ msg: '\\n    line1\\n    line2\\n' }";

        var config = new ConfigurationBuilder()
            .AddJson5File(new InMemoryFileProvider(json5), "test.json5", options: new Json5ReaderOptions { AutoDedent = true })
            .Build();

        Assert.Equal("line1\nline2", config["msg"]);
    }

    static IConfiguration LoadFromJson5(string json5)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json5));

        return new ConfigurationBuilder()
            .AddJson5Stream(stream)
            .Build();
    }
}
