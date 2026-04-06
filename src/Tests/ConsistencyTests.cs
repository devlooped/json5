using System.Text.Json;
using System.Text.Json.Nodes;
using Json5;
using J5 = Json5.Json5;

namespace Tests;

/// <summary>
/// Tests verifying that all API paths produce consistent results for the same input.
/// </summary>
public class ConsistencyTests
{
    const string SampleJson5 = """
        {
            // A sample JSON5 document
            name: 'test',
            version: 1,
            tags: ['a', 'b', 'c'],
            nested: {
                hex: 0xFF,
                float: .5,
                flag: true,
                nothing: null,
            },
        }
        """;

    [Fact]
    public void AllPathsParseEquivalently()
    {
        // Parse path (JsonNode)
        var node = J5.Parse(SampleJson5);
        Assert.NotNull(node);
        Assert.Equal("test", node!["name"]!.GetValue<string>());
        Assert.Equal(1L, node!["version"]!.GetValue<long>());
        Assert.Equal(3, node!["tags"]!.AsArray().Count);
        Assert.Equal(255L, node!["nested"]!["hex"]!.GetValue<long>());
        Assert.Equal(0.5, node!["nested"]!["float"]!.GetValue<double>());
        Assert.True(node!["nested"]!["flag"]!.GetValue<bool>());
        Assert.Null(node!["nested"]!["nothing"]);

        // Deserialize path
        var dict = J5.Deserialize<Dictionary<string, JsonElement>>(SampleJson5);
        Assert.NotNull(dict);
        Assert.Equal("test", dict!["name"].GetString());

        // ParseDocument path
        using var doc = J5.ParseDocument(SampleJson5);
        Assert.Equal("test", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal(1, doc.RootElement.GetProperty("version").GetInt32());

        // ToJson path — must produce valid JSON
        var json = J5.ToJson(SampleJson5);
        using var doc2 = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal("test", doc2.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public void Json5ToJsonRoundTripPreservesStructure()
    {
        // Parse JSON5 → JsonNode → serialize to JSON → parse JSON → compare
        var node = J5.Parse(SampleJson5);
        var json = node!.ToJsonString();

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal("test", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal(3, doc.RootElement.GetProperty("tags").GetArrayLength());
    }
}
