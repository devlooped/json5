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
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddJson5File(this IConfigurationBuilder builder, string path)
        => AddJson5File(builder, provider: null, path: path, optional: false, reloadOnChange: false);

    /// <summary>
    /// Adds a JSON5 configuration source to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="path">Path relative to the base path stored in
    /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddJson5File(this IConfigurationBuilder builder, string path, bool optional)
        => AddJson5File(builder, provider: null, path: path, optional: optional, reloadOnChange: false);

    /// <summary>
    /// Adds a JSON5 configuration source to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="path">Path relative to the base path stored in
    /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddJson5File(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange)
        => AddJson5File(builder, provider: null, path: path, optional: optional, reloadOnChange: reloadOnChange);

    /// <summary>
    /// Adds a JSON5 configuration source to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="provider">The <see cref="IFileProvider"/> to use to access the file.</param>
    /// <param name="path">Path relative to the base path stored in
    /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddJson5File(this IConfigurationBuilder builder, IFileProvider? provider, string path, bool optional, bool reloadOnChange)
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
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddJson5Stream(this IConfigurationBuilder builder, Stream stream)
    {
        ThrowIfNull(builder);

        return builder.Add<Json5StreamConfigurationSource>(s => s.Stream = stream);
    }
}
