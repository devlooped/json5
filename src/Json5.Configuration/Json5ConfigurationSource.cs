using Microsoft.Extensions.Configuration;

namespace Json5;

/// <summary>
/// Represents a JSON5 file as an <see cref="IConfigurationSource"/>.
/// </summary>
public class Json5ConfigurationSource : FileConfigurationSource
{
    /// <summary>
    /// Gets or sets the <see cref="Json5ReaderOptions"/> to use when parsing the JSON5 file.
    /// </summary>
    public Json5ReaderOptions Json5ReaderOptions { get; set; }

    /// <summary>
    /// Builds the <see cref="Json5ConfigurationProvider"/> for this source.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
    /// <returns>A <see cref="Json5ConfigurationProvider"/>.</returns>
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        EnsureDefaults(builder);
        return new Json5ConfigurationProvider(this);
    }
}
