using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using HCMv3.Security;

namespace HCMv3.Administration.Tools;

/// <inheritdoc cref="ICredentialPasswordGenerator"/>
public class CredentialPasswordGenerator : HCMv3DomainServiceBase, ICredentialPasswordGenerator
{
    /// <summary>
    /// Never shorter than this, whatever the tenant's minimum is: the password travels by e-mail, and a short
    /// one adds nothing for the user, who must replace it at the next sign-in anyway.
    /// </summary>
    public const int MinimumLength = 12;

    // Characters that are easy to misread in an e-mail (I, l, O, 0, 1) are left out.
    private const string UpperCaseLetters = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string LowerCaseLetters = "abcdefghijkmnpqrstuvwxyz";
    private const string Digits = "23456789";
    private const string NonAlphanumerics = "!@$?_-";

    private static readonly string[] AllSets = { UpperCaseLetters, LowerCaseLetters, Digits, NonAlphanumerics };

    private readonly IPasswordComplexitySettingStore _complexitySettingStore;

    public CredentialPasswordGenerator(IPasswordComplexitySettingStore complexitySettingStore)
    {
        _complexitySettingStore = complexitySettingStore;
    }

    public async Task<string> CreateAsync()
    {
        var settings = await _complexitySettingStore.GetSettingsAsync();

        var chars = new List<char>();

        // One of each required kind first, the same shape as Zero's CreateRandomPassword.
        if (settings.RequireUppercase)
        {
            chars.Add(Pick(UpperCaseLetters));
        }

        if (settings.RequireLowercase)
        {
            chars.Add(Pick(LowerCaseLetters));
        }

        if (settings.RequireDigit)
        {
            chars.Add(Pick(Digits));
        }

        if (settings.RequireNonAlphanumeric)
        {
            chars.Add(Pick(NonAlphanumerics));
        }

        var length = Math.Max(settings.RequiredLength, MinimumLength);

        while (chars.Count < length)
        {
            chars.Add(Pick(AllSets[RandomNumberGenerator.GetInt32(AllSets.Length)]));
        }

        // Fisher-Yates, so the required characters are not always at the front.
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars.ToArray());
    }

    private static char Pick(string set)
    {
        return set[RandomNumberGenerator.GetInt32(set.Length)];
    }
}
