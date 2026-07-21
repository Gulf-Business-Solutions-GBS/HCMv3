using System.Threading.Tasks;

namespace HCMv3.Security.Recaptcha;

public interface IRecaptchaValidator
{
    Task ValidateAsync(string captchaResponse);
}
