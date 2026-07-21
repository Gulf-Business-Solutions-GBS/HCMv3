using Abp.Dependency;
using System.Collections.Generic;

namespace HCMv3.Storage.FileValidator;

public class WordFileValidator : BaseFileValidator, ITransientDependency
{
    protected override HashSet<string> AllowedExtensions => new()
    {
        ".docx", ".doc", ".dot", ".dotx", ".rtf", ".odt"
    };

    protected override HashSet<string> AllowedMimeTypes => new()
    {
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.template",
        "application/rtf",
        "text/rtf",
        "application/vnd.oasis.opendocument.text"
    };

    protected override Dictionary<string, List<byte[]>> AllowedFileSignatures => new()
    {
        { ".docx", new() { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
        { ".doc",  new() { new byte[] { 0xD0, 0xCF, 0x11, 0xE0 } } },
        { ".dot",  new() { new byte[] { 0xD0, 0xCF, 0x11, 0xE0 } } },
        { ".dotx", new() { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
        { ".rtf",  new() { new byte[] { 0x7B, 0x5C, 0x72, 0x74, 0x66 } } },
        { ".odt",  new() { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } }
    };
}