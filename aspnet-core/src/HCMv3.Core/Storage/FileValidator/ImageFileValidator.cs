using Abp.Dependency;
using System.Collections.Generic;

namespace HCMv3.Storage.FileValidator;

public class ImageFileValidator : BaseFileValidator, IFileValidator, ITransientDependency
{
    protected override HashSet<string> AllowedExtensions => new()
    {
        ".jpeg", ".jpg", ".png", ".webp", ".gif"
    };

    protected override HashSet<string> AllowedMimeTypes => new()
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    protected override Dictionary<string, List<byte[]>> AllowedFileSignatures => new()
    {
        { ".gif", new() { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
        { ".jpeg", new() { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".jpg", new() { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".png", new() { new byte[] { 0x89, 0x50, 0x4E, 0x47 } } },
    };
}