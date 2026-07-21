using Abp.Dependency;
using Abp.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HCMv3.Storage.FileValidator;

public class CssFileValidator : BaseFileValidator, ITransientDependency
{
    protected override HashSet<string> AllowedExtensions => new()
    {
        ".css"
    };

    protected override HashSet<string> AllowedMimeTypes => new()
    {
        "text/css"
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

        if (!(content.Contains("{") && content.Contains("}")))
        {
            throw new Exception(L("InvalidCSSContent"));
        }
    }
}
