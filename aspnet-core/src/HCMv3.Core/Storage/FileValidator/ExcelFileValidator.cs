using Abp.Dependency;
using System.Collections.Generic;

namespace HCMv3.Storage.FileValidator;

public class ExcelFileValidator : BaseFileValidator, ITransientDependency
{
    protected override HashSet<string> AllowedExtensions => new()
    {
        ".xlsx", ".xls", ".xlsm", ".xltx", ".xlt", ".ods"
    };

    protected override HashSet<string> AllowedMimeTypes => new()
    {
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-excel",
        "application/vnd.ms-excel.sheet.macroEnabled.12",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.template",
        "application/vnd.oasis.opendocument.spreadsheet"
    };

    protected override Dictionary<string, List<byte[]>> AllowedFileSignatures => new()
    {
        { ".xlsx", new() { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
        { ".xls",  new() { new byte[] { 0xD0, 0xCF, 0x11, 0xE0 } } },
        { ".xlsm", new() { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
        { ".xltx", new() { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
        { ".xlt",  new() { new byte[] { 0xD0, 0xCF, 0x11, 0xE0 } } },
        { ".ods",  new() { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } }
    };
}