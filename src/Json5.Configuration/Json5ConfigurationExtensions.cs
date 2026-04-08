using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Json5;

/// <summary>
/// Extension methods for adding <see cref="Json5ConfigurationProvider"/> and
/// <see cref="Json5StreamConfigurationProvider"/> to an <see cref="IConfigurationBuilder"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class Json5ConfigurationExtensions
{
    /// <summary>
    /// Adds a JSON5 configuration source to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="path">Path relative to the base path stored in
    /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
    /// <param name="options">The <see cref="Json5ReaderOptions"/> to use when reading the JSON5 data.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddJson5File(this IConfigurationBuilder builder, string path, bool optional = false, bool reloadOnChange = false, Json5ReaderOptions? options = null)
        => AddJson5File(builder, null, path, optional, reloadOnChange, options);

    /// <summary>
    /// Adds a JSON5 configuration source to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="provider">The <see cref="IFileProvider"/> to use to access the file.</param>
    /// <param name="path">Path relative to the base path stored in
    /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
    /// <param name="options">The <see cref="Json5ReaderOptions"/> to use when reading the JSON5 data.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddJson5File(this IConfigurationBuilder builder, IFileProvider? provider, string path, bool optional = false, bool reloadOnChange = false, Json5ReaderOptions? options = null)
    {
        ThrowIfNull(builder);

        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("File path must be a non-empty string.", nameof(path));

        return builder.AddJson5File(s =>
        {
            s.FileProvider = provider;
            s.Path = path;
            s.Optional = optional;
            s.ReloadOnChange = reloadOnChange;
            if (options != null)
                s.Json5ReaderOptions = options.Value;

            s.ResolveFileProvider();
        });
    }

    /// <summary>
    /// Adds a JSON5 configuration source to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="configureSource">Configures the source.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddJson5File(this IConfigurationBuilder builder, Action<Json5ConfigurationSource>? configureSource)
        => builder.Add(configureSource);

    /// <summary>
    /// Adds a JSON5 configuration source to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="stream">The <see cref="Stream"/> to read the JSON5 data from.</param>
    /// <param name="options">The <see cref="Json5ReaderOptions"/> to use when reading the JSON5 data.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddJson5Stream(this IConfigurationBuilder builder, Stream stream, Json5ReaderOptions? options = null)
    {
        ThrowIfNull(builder);

        return builder.Add<Json5StreamConfigurationSource>(s =>
        {
            s.Stream = stream;
            if (options != null)
                s.Json5ReaderOptions = options.Value;
        });
    }
}
