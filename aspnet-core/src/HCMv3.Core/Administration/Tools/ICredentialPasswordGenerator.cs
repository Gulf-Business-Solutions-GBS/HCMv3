using System.Threading.Tasks;

namespace HCMv3.Administration.Tools;

/// <summary>
/// Makes the temporary password the Reset User Passwords tool e-mails to a user.
///
/// <para>
/// It follows the tenant's own password complexity settings, read through ASP.NET Zero's
/// <see cref="HCMv3.Security.IPasswordComplexitySettingStore"/>, exactly like Zero's
/// <c>UserManager.CreateRandomPassword()</c>. It exists only because that method seeds
/// <c>System.Random</c> with <c>Environment.TickCount</c>: called in a loop over many users it hands
/// several of them the same password whenever two calls land in the same clock tick (about 15 ms), and a
/// tick-count seed can be guessed. A bulk tool calls it in exactly that loop, so this one draws from
/// <see cref="System.Security.Cryptography.RandomNumberGenerator"/> instead. The result is still checked
/// by Zero's own password validators before it is used.
/// </para>
/// </summary>
public interface ICredentialPasswordGenerator
{
    Task<string> CreateAsync();
}
