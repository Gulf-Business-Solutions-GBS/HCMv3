using System.Collections.Generic;
using System.IO;

namespace HCMv3.Storage.FileValidator;
public interface IFileValidateInput
{
    string FileName { get; }
    string ContentType { get; }
    long Length { get; }
    Stream OpenReadStream();
}

