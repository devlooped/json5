using System.Text.Json;
using System.Text.Json.Nodes;

namespace Json5;

/// <summary>
/// Options that control how JSON5 input is parsed.
/// </summary>
public record struct Json5ReaderOptions
{
    /// <summary>
    /// How to handle <c>Infinity</c>, <c>-Infinity</c>, and <c>NaN</c> literals.
    /// Default is <see cref="SpecialNumberHandling.AsString"/>.
    /// </summary>
    public SpecialNumberHandling SpecialNumbers { get; set; }

    /// <summary>
    /// Maximum nesting depth allowed. Default is 64.
    /// </summary>
    public int MaxDepth { get; set; }

    /// <summary>
    /// Creates a new instance with default values.
    /// </summary>
    public Json5ReaderOptions()
    {
        SpecialNumbers = SpecialNumberHandling.AsString;
        MaxDepth = 64;
    }
}

/// <summary>
/// Controls how IEEE 754 special values (Infinity, NaN) are represented.
/// </summary>
public enum SpecialNumberHandling
{
    /// <summary>
    /// Represent as <see cref="JsonValue"/> strings: <c>"Infinity"</c>, <c>"-Infinity"</c>, <c>"NaN"</c>.
    /// Works with <see cref="JsonNumberHandling.AllowNamedFloatingPointLiterals"/>.
    /// </summary>
    AsString,

    /// <summary>
    /// Represent as <c>null</c> in the resulting <see cref="JsonNode"/> tree.
    /// </summary>
    AsNull,

    /// <summary>
    /// Throw a <see cref="Json5Exception"/> when encountered.
    /// </summary>
    Throw,
}
