using System.Threading.Tasks;
using HCMv3.Security.Recaptcha;

namespace HCMv3.Test.Base.Web;

public class FakeRecaptchaValidator : IRecaptchaValidator
{
    public Task ValidateAsync(string captchaResponse)
    {
        return Task.CompletedTask;
    }
}
