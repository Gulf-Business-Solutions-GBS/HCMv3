using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.Net.Emailing;

public interface IEmailSettingsChecker
{
    bool EmailSettingsValid();

    Task<bool> EmailSettingsValidAsync();

    /// <summary>
    /// The settings that are missing, empty, or still holding the placeholder values ASP.NET Zero
    /// seeds. Returns localization keys (for example "SmtpHost"), so the caller decides the language.
    /// Empty means the settings are usable.
    /// </summary>
    IReadOnlyList<string> GetMissingEmailSettingKeys();

    /// <inheritdoc cref="GetMissingEmailSettingKeys"/>
    Task<IReadOnlyList<string>> GetMissingEmailSettingKeysAsync();
}
