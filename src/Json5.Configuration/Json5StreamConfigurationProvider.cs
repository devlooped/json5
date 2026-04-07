using Microsoft.Extensions.Configuration;

namespace Json5;

/// <summary>
/// A JSON5 stream-based <see cref="StreamConfigurationProvider"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance with the specified source.
/// </remarks>
/// <param name="source">The source settings.</param>
public class Json5StreamConfigurationProvider(Json5StreamConfigurationSource source) : StreamConfigurationProvider(source)
{
    /// <summary>
    /// Loads the JSON5 data from a stream.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    public override void Load(Stream stream) =>
        Data = Json5ConfigurationFileParser.Parse(stream, ((Json5StreamConfigurationSource)Source).Json5ReaderOptions);
}
