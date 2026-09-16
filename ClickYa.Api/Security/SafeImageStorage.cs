namespace ClickYa.Api.Security;

public sealed class SafeImageStorage
{
    private const long MaxImageBytes = 10_000_000;
    private readonly string _uploadsRoot;

    public SafeImageStorage(IWebHostEnvironment environment) =>
        _uploadsRoot = Path.Combine(environment.WebRootPath, "uploads");

    public async Task<string?> SaveAsync(IFormFile file, string? subdirectory = null)
    {
        var validated = await ReadValidated(file);
        return validated == null ? null : await Write(validated.Value.data, validated.Value.extension, subdirectory);
    }

    public async Task<List<string>?> SaveManyAsync(IEnumerable<IFormFile>? files, int maxFiles)
    {
        var source = files?.ToList() ?? new List<IFormFile>();
        if (source.Count > maxFiles) return null;

        var validated = new List<(byte[] data, string extension)>();
        foreach (var file in source)
        {
            var item = await ReadValidated(file);
            if (item == null) return null;
            validated.Add(item.Value);
        }

        var urls = new List<string>();
        foreach (var item in validated)
            urls.Add(await Write(item.data, item.extension, null));
        return urls;
    }

    public void Delete(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl) ||
            !relativeUrl.StartsWith("/uploads/", StringComparison.Ordinal) ||
            relativeUrl.Contains("..", StringComparison.Ordinal)) return;
        var relativePath = relativeUrl["/uploads/".Length..].Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_uploadsRoot, relativePath));
        var root = Path.GetFullPath(_uploadsRoot) + Path.DirectorySeparatorChar;
        if (fullPath.StartsWith(root, StringComparison.Ordinal) && File.Exists(fullPath)) File.Delete(fullPath);
    }

    private static async Task<(byte[] data, string extension)?> ReadValidated(IFormFile file)
    {
        if (file.Length <= 0 || file.Length > MaxImageBytes) return null;
        await using var input = file.OpenReadStream();
        using var memory = new MemoryStream();
        await input.CopyToAsync(memory);
        var data = memory.ToArray();
        var extension = DetectExtension(data);
        return extension == null ? null : (data, extension);
    }

    private async Task<string> Write(byte[] data, string extension, string? subdirectory)
    {
        if (!string.IsNullOrEmpty(subdirectory) &&
            (!string.Equals(Path.GetFileName(subdirectory), subdirectory, StringComparison.Ordinal) ||
             subdirectory.Contains("..", StringComparison.Ordinal)))
            throw new InvalidOperationException("Subdirectorio de imágenes inválido.");

        var directory = string.IsNullOrEmpty(subdirectory)
            ? _uploadsRoot : Path.Combine(_uploadsRoot, subdirectory);
        Directory.CreateDirectory(directory);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await File.WriteAllBytesAsync(Path.Combine(directory, fileName), data);
        return string.IsNullOrEmpty(subdirectory)
            ? $"/uploads/{fileName}"
            : $"/uploads/{subdirectory}/{fileName}";
    }

    private static string? DetectExtension(byte[] data)
    {
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF) return ".jpg";
        if (data.Length >= 8 && data.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ".png";
        if (data.Length >= 12 && System.Text.Encoding.ASCII.GetString(data, 0, 4) == "RIFF" &&
            System.Text.Encoding.ASCII.GetString(data, 8, 4) == "WEBP") return ".webp";
        return null;
    }
}
