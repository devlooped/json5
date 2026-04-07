using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Tests;

/// <summary>
/// A simple in-memory file provider for testing file-based configuration sources.
/// </summary>
class InMemoryFileProvider : IFileProvider
{
    readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryFileProvider(string content) => _files["test.json5"] = content;

    public InMemoryFileProvider(string path, string content) => _files[path] = content;

    public IDirectoryContents GetDirectoryContents(string subpath) =>
        NotFoundDirectoryContents.Singleton;

    public IFileInfo GetFileInfo(string subpath) =>
        _files.TryGetValue(subpath.TrimStart('/'), out var content)
            ? new InMemoryFileInfo(subpath, content)
            : new NotFoundFileInfo(subpath);

    public IChangeToken Watch(string filter) =>
        NullChangeToken.Singleton;

    class InMemoryFileInfo(string path, string content) : IFileInfo
    {
        readonly byte[] _bytes = Encoding.UTF8.GetBytes(content);

        public bool Exists => true;
        public long Length => _bytes.Length;
        public string? PhysicalPath => null;
        public string Name => Path.GetFileName(path);
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public bool IsDirectory => false;

        public Stream CreateReadStream() => new MemoryStream(_bytes);
    }
}
