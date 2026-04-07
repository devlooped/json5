using System.Diagnostics;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

namespace Json5;

sealed class Json5ConfigurationFileParser
{
    readonly Dictionary<string, string?> _data = new(StringComparer.OrdinalIgnoreCase);
    readonly Stack<string> _paths = new();

    Json5ConfigurationFileParser() { }

    public static IDictionary<string, string?> Parse(Stream input, Json5ReaderOptions json5Options = default)
    {
        var parser = new Json5ConfigurationFileParser();
        parser.ParseStream(input, json5Options);
        return parser._data;
    }

    void ParseStream(Stream input, Json5ReaderOptions json5Options)
    {
        using var ms = new MemoryStream();
        input.CopyTo(ms);
        var bytes = ms.ToArray();

        var root = Json5.Parse(bytes.AsSpan(), json5Options);

        if (root is not JsonObject obj)
            throw new FormatException("Top-level JSON5 element must be an object.");

        VisitObjectNode(obj);
    }

    void VisitObjectNode(JsonObject obj)
    {
        foreach (var property in obj)
        {
            EnterContext(property.Key);
            VisitNode(property.Value);
            ExitContext();
        }
    }

    void VisitArrayNode(JsonArray array)
    {
        for (var index = 0; index < array.Count; index++)
        {
            EnterContext(index.ToString());
            VisitNode(array[index]);
            ExitContext();
        }
    }

    void VisitNode(JsonNode? node)
    {
        Debug.Assert(_paths.Count > 0);

        switch (node)
        {
            case JsonObject obj:
                if (obj.Count == 0)
                {
                    // Empty object, store as null (matches M.E.Configuration.Json behavior)
                    var key = _paths.Peek();
                    _data[GetCurrentPath()] = null;
                }
                else
                {
                    VisitObjectNode(obj);
                }
                break;

            case JsonArray array:
                if (array.Count == 0)
                {
                    // Empty array, store as empty string (matches M.E.Configuration.Json behavior)
                    _data[GetCurrentPath()] = string.Empty;
                }
                else
                {
                    VisitArrayNode(array);
                }
                break;

            case JsonValue value:
                SetValue(value);
                break;

            case null:
                var path = GetCurrentPath();
                if (_data.ContainsKey(path))
                    throw new FormatException($"A duplicate key '{path}' was found.");
                _data[path] = null;
                break;
        }
    }

    void SetValue(JsonValue value)
    {
        var path = GetCurrentPath();
        if (_data.ContainsKey(path))
            throw new FormatException($"A duplicate key '{path}' was found.");

        _data[path] = value.ToString();
    }

    void EnterContext(string context)
        => _paths.Push(_paths.Count > 0 ? _paths.Peek() + ConfigurationPath.KeyDelimiter + context : context);

    void ExitContext()
        => _paths.Pop();

    string GetCurrentPath()
        => _paths.Peek();
}
