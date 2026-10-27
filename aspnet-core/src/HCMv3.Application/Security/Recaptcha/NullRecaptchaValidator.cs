using System.Threading.Tasks;

namespace HCMv3.Security.Recaptcha;

public class NullRecaptchaValidator : IRecaptchaValidator
{
    public static NullRecaptchaValidator Instance { get; } = new NullRecaptchaValidator();

    public Task ValidateAsync(string captchaResponse)
    {
        return Task.CompletedTask;
    }
}
