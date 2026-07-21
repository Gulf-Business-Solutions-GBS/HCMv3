using Abp.Dependency;
using System.Collections.Generic;

namespace HCMv3.Storage.FileValidator;

public class PdfFileValidator : BaseFileValidator, ITransientDependency
{
    protected override HashSet<string> AllowedExtensions => new() { ".pdf" };

    protected override HashSet<string> AllowedMimeTypes => new()
    {
        "application/pdf"
    };

    protected override Dictionary<string, List<byte[]>> AllowedFileSignatures => new()
    {
        { ".pdf", new() { new byte[] { 0x25, 0x50, 0x44, 0x46 } } }
    };
}