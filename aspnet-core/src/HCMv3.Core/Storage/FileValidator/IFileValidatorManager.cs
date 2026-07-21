namespace HCMv3.Storage.FileValidator;
public interface IFileValidatorManager
{
    (bool Success, string Message) ValidateAll(IFileValidateInput file);
    void ValidateFile<TValidator>(IFileValidateInput file) where TValidator : IFileValidator;
}
