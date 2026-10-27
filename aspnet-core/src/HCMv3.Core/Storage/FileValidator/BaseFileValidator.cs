using Abp.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HCMv3.Storage.FileValidator;

public abstract class BaseFileValidator : HCMv3ServiceBase, IFileValidator
{
    protected abstract HashSet<string> AllowedExtensions { get; }
    protected abstract HashSet<string> AllowedMimeTypes { get; }
    protected abstract Dictionary<string, List<byte[]>> AllowedFileSignatures { get; }

    public virtual bool CanValidate(IFileValidateInput file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return AllowedExtensions.Contains(ext) &&
               AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant());
    }

    public virtual void Validate(IFileValidateInput file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (AllowedFileSignatures != null && AllowedFileSignatures.TryGetValue(ext, out var signatures))
        {
            using var stream = file.OpenReadStream();
            var header = new byte[signatures.Max(s => s.Length)];
            stream.ReadExactly(header, 0, header.Length);

            if (!signatures.Any(sig => header.Take(sig.Length).SequenceEqual(sig)))
                throw new UserFriendlyException(L("FileSignatureMismatchOrInvalidContent"));
        }
    }

    protected void ValidateCommonSecurity(string content)
    {
        if (content.Contains("<script", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("javascript:", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("onerror=", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("onload=", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception(L("UnsafeFileContent"));
        }
    }
}
