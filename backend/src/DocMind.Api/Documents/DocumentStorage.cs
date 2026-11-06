using Microsoft.Extensions.Options;

namespace DocMind.Api.Documents;

public sealed class DocumentStorage
{
    private readonly string _rootPath;

    public DocumentStorage(IOptions<DocumentStorageOptions> options, IHostEnvironment environment)
    {
        _rootPath = Path.GetFullPath(options.Value.RootPath, environment.ContentRootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public string GetPath(string storageName)
    {
        if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(storageName), "N", out _))
        {
            throw new InvalidOperationException("The stored document key is invalid.");
        }

        var path = Path.GetFullPath(Path.Combine(_rootPath, storageName));
        if (!path.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The stored document path is invalid.");
        }

        return path;
    }

    public async Task SaveAsync(string storageName, Stream source, CancellationToken cancellationToken)
    {
        var path = GetPath(storageName);
        await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await source.CopyToAsync(target, cancellationToken);
    }

    public void Delete(string storageName)
    {
        var path = GetPath(storageName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
