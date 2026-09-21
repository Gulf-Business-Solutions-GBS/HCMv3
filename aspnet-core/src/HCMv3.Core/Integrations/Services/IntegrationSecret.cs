using System;
using Abp.Runtime.Security;

namespace HCMv3.Integrations.Services;

/// <summary>
/// Encrypts and decrypts integration credentials.
///
/// Deliberately one shared helper rather than encrypt/decrypt calls written at each call site, because a
/// credential column is usually touched from more than one place -- <c>TimeAttAPI.Password</c>, for
/// instance, is read by the Integration Center, by the time-attendance sync service and by the existing
/// Time Attendance API screen. If those disagree about whether a value is already encrypted, the result
/// is a double-encrypted password that fails authentication at 2am with no clue why.
///
/// Uses <c>SimpleStringCipher</c>, which is already how this application protects the SMTP password and
/// tenant connection strings.
/// </summary>
public static class IntegrationSecret
{
    /// <summary>Encrypts a plain-text value for storage. Null and blank pass through unchanged.</summary>
    public static string Protect(string plainText)
    {
        return string.IsNullOrEmpty(plainText)
            ? plainText
            : SimpleStringCipher.Instance.Encrypt(plainText);
    }

    /// <summary>
    /// Decrypts a stored value.
    ///
    /// TOLERANT BY DESIGN: a value that is not valid ciphertext is returned as-is rather than throwing.
    /// Columns being brought under encryption still hold plain text written before the change, and a
    /// tenant whose integration stops working is a worse outcome than one whose old value is re-encrypted
    /// on its next save.
    /// </summary>
    public static string Reveal(string storedValue)
    {
        if (string.IsNullOrEmpty(storedValue))
        {
            return storedValue;
        }

        try
        {
            return SimpleStringCipher.Instance.Decrypt(storedValue);
        }
        catch (Exception)
        {
            return storedValue;
        }
    }
}
