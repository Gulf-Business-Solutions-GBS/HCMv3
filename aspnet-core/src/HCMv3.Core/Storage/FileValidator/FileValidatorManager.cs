using Abp.Dependency;
using Abp.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HCMv3.Storage.FileValidator;

public class FileValidatorManager : HCMv3ServiceBase, IFileValidatorManager, ITransientDependency
{
    private readonly IEnumerable<IFileValidator> _validators;

    public FileValidatorManager(IEnumerable<IFileValidator> validators)
    {
        _validators = validators;
    }

    public (bool Success, string Message) ValidateAll(IFileValidateInput file)
    {
        foreach (var validator in _validators)
        {
            if (validator.CanValidate(file))
            {
                try
                {
                    validator.Validate(file);
                    return (true, L("ValidationSuccessful"));
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            }
        }

        return (false, L("NoSuitableValidatorFound"));
    }

    public void ValidateFile<TValidator>(IFileValidateInput file) where TValidator : IFileValidator
    {
        var validator = _validators.OfType<TValidator>().FirstOrDefault();

        if (validator == null)
            throw new Exception(L("ValidatorNotFoundForType", typeof(TValidator).Name));

        if (!validator.CanValidate(file))
            throw new UserFriendlyException(L("UnsupportedFileTypeForForm"));

        validator.Validate(file);
    }
}