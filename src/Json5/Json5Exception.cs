using System.Text.Json;

namespace Json5;

/// <summary>
/// Exception thrown when JSON5 input is malformed or violates parser constraints.
/// </summary>
public class Json5Exception : JsonException
{
    /// <summary>
    /// The 0-based byte position within the input where the error occurred.
    /// </summary>
    public long Position { get; }

    /// <summary>
    /// The 1-based line number where the error occurred.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// The 1-based column number where the error occurred.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Creates a new <see cref="Json5Exception"/> with positional information.
    /// </summary>
    public Json5Exception(string message, long position, int line, int column)
        : base(message)
    {
        Position = position;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Creates a new <see cref="Json5Exception"/> with positional information and an inner exception.
    /// </summary>
    public Json5Exception(string message, long position, int line, int column, Exception innerException)
        : base(message, innerException)
    {
        Position = position;
        Line = line;
        Column = column;
    }
}
