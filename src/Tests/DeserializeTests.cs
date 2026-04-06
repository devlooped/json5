using System.Text.Json;
using Json5;
using J5 = Json5.Json5;

namespace Tests;

public class DeserializeTests
{
    [Fact]
    public void DeserializeSimpleObject()
    {
        var result = J5.Deserialize<Dictionary<string, int>>("{a: 1, b: 2}");
        Assert.NotNull(result);
        Assert.Equal(1, result!["a"]);
        Assert.Equal(2, result!["b"]);
    }

    [Fact]
    public void DeserializeTypedObject()
    {
        var result = J5.Deserialize<Person>("{Name: 'Alice', Age: 30}");
        Assert.NotNull(result);
        Assert.Equal("Alice", result!.Name);
        Assert.Equal(30, result!.Age);
    }

    [Fact]
    public void DeserializeArray()
    {
        var result = J5.Deserialize<int[]>("[1, 2, 3]");
        Assert.NotNull(result);
        Assert.Equal([1, 2, 3], result!);
    }

    [Fact]
    public void DeserializeWithHexNumber()
    {
        var result = J5.Deserialize<Dictionary<string, int>>("{value: 0xFF}");
        Assert.Equal(255, result!["value"]);
    }

    [Fact]
    public void DeserializeWithSingleQuotedStrings()
    {
        var result = J5.Deserialize<string[]>("['a', 'b', 'c']");
        Assert.Equal(["a", "b", "c"], result!);
    }

    [Fact]
    public void DeserializeWithComments()
    {
        var json5 = """
            {
                // This is a comment
                name: 'test',
                /* Another comment */
                value: 42,
            }
            """;
        var result = J5.Deserialize<Dictionary<string, JsonElement>>(json5);
        Assert.NotNull(result);
    }

    [Fact]
    public void DeserializeNonGeneric()
    {
        var result = J5.Deserialize("{x: 1}", typeof(Dictionary<string, int>));
        var dict = Assert.IsType<Dictionary<string, int>>(result);
        Assert.Equal(1, dict["x"]);
    }

    [Fact]
    public void DeserializeWithJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = J5.Deserialize<Person>("{Name: 'Bob', Age: 25}", options);
        Assert.Equal("Bob", result!.Name);
        Assert.Equal(25, result!.Age);
    }

    public record Person
    {
        public string Name { get; init; } = "";
        public int Age { get; init; }
    }
}
