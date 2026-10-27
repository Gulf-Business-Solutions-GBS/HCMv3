using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;

namespace HCMv3.Storage.FileValidator;

public class FileValidateInput : IFileValidateInput
{
    private readonly IFormFile _formFile;

    public FileValidateInput(IFormFile formFile)
    {
        _formFile = formFile;
    }

    public string FileName => _formFile.FileName;
    public string ContentType => _formFile.ContentType;
    public long Length => _formFile.Length;
    public Stream OpenReadStream() => _formFile.OpenReadStream();
}

