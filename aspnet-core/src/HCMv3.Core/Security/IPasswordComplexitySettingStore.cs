using System.Threading.Tasks;

namespace HCMv3.Security;

public interface IPasswordComplexitySettingStore
{
    Task<PasswordComplexitySetting> GetSettingsAsync();
}

