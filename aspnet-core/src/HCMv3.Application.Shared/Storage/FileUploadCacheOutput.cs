using Abp.Web.Models;

namespace HCMv3.Storage;

public class FileUploadCacheOutput : ErrorInfo
{
    public string FileToken { get; set; }

    public FileUploadCacheOutput() { }

    public FileUploadCacheOutput(string fileToken)
    {
        FileToken = fileToken;
    }

    public FileUploadCacheOutput(ErrorInfo error)
    {
        Code = error.Code;
        Details = error.Details;
        Message = error.Message;
        ValidationErrors = error.ValidationErrors;
    }
}