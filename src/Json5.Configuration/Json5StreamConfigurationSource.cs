using Microsoft.Extensions.Configuration;

namespace Json5;

/// <summary>
/// Represents a JSON5 stream as an <see cref="IConfigurationSource"/>.
/// </summary>
public class Json5StreamConfigurationSource : StreamConfigurationSource
{
    /// <summary>
    /// Gets or sets the <see cref="Json5ReaderOptions"/> to use when parsing the JSON5 stream.
    /// </summary>
    public Json5ReaderOptions Json5ReaderOptions { get; set; }

    /// <summary>
    /// Builds the <see cref="Json5StreamConfigurationProvider"/> for this source.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
    /// <returns>A <see cref="Json5StreamConfigurationProvider"/>.</returns>
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
        => new Json5StreamConfigurationProvider(this);
}