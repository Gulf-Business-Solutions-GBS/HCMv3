using System.Threading.Tasks;

namespace HCMv3.Net.Emailing;

public interface IEmailSettingsChecker
{
    bool EmailSettingsValid();

    Task<bool> EmailSettingsValidAsync();
}

