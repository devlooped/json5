using Microsoft.Extensions.Configuration;

namespace Json5;

/// <summary>
/// A JSON5 file-based <see cref="FileConfigurationProvider"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance with the specified source.
/// </remarks>
/// <param name="source">The source settings.</param>
public class Json5ConfigurationProvider(Json5ConfigurationSource source) : FileConfigurationProvider(source)
{
    /// <summary>
    /// Loads the JSON5 data from a stream.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    public override void Load(Stream stream)
    {
        try
        {
            Data = Json5ConfigurationFileParser.Parse(stream, ((Json5ConfigurationSource)Source).Json5ReaderOptions);
        }
        catch (Json5Exception ex)
        {
            throw new FormatException($"Could not parse the JSON5 file. Error on line {ex.Line}, column {ex.Column}.", ex);
        }
    }
}
