using FuzulTaksitTakip.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace FuzulTaksitTakip.Infrastructure.Storage;

public sealed class LocalReceiptStorage : IReceiptStorage
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf"
    };

    private readonly ReceiptStorageOptions _options;

    public LocalReceiptStorage(IOptions<ReceiptStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> SaveAsync(
        Guid planId,
        Guid paymentId,
        Stream content,
        string contentType,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException("Desteklenmeyen dosya tipi. JPEG, PNG, WebP veya PDF yükleyin.");
        }

        if (content.CanSeek && content.Length > _options.MaxBytes)
        {
            throw new InvalidOperationException($"Dekont en fazla {_options.MaxBytes / (1024 * 1024)} MB olabilir.");
        }

        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 10)
        {
            ext = contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "application/pdf" => ".pdf",
                _ => ".bin"
            };
        }

        var root = Path.GetFullPath(_options.ReceiptsPath);
        var planDir = Path.Combine(root, planId.ToString("N"));
        Directory.CreateDirectory(planDir);

        var fileName = $"{paymentId:N}_{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(planDir, fileName);
        var relativeKey = Path.Combine(planId.ToString("N"), fileName).Replace('\\', '/');

        await using var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fs, cancellationToken);

        if (fs.Length > _options.MaxBytes)
        {
            fs.Close();
            File.Delete(fullPath);
            throw new InvalidOperationException($"Dekont en fazla {_options.MaxBytes / (1024 * 1024)} MB olabilir.");
        }

        return relativeKey;
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return Task.CompletedTask;
        }

        var root = Path.GetFullPath(_options.ReceiptsPath);
        var fullPath = Path.GetFullPath(Path.Combine(root, storageKey));
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
