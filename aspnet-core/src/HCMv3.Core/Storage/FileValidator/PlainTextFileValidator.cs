using Abp.Dependency;
using Abp.UI;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HCMv3.Storage.FileValidator;

public class PlainTextFileValidator : BaseFileValidator, ITransientDependency
{
    protected override HashSet<string> AllowedExtensions => new()
    {
        ".txt", ".csv", ".md"
    };

    protected override HashSet<string> AllowedMimeTypes => new()
    {
        "text/plain",
        "text/csv",
        "text/markdown"
    };

    protected override Dictionary<string, List<byte[]>> AllowedFileSignatures => null;

    public override void Validate(IFileValidateInput file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new UserFriendlyException(L("InvalidFileType"));

        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, true);
        var content = reader.ReadToEnd();

        if (string.IsNullOrWhiteSpace(content))
            throw new UserFriendlyException(L("EmptyOrInvalidFile"));

        ValidateCommonSecurity(content);
    }
}
