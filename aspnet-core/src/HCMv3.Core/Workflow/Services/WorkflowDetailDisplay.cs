using System.Globalization;

namespace HCMv3.Workflow.Services;

/// <summary>
/// How a handler's <c>BuildDetailAsync</c> words a lookup for whoever is reading: the Arabic name for an
/// Arabic reader, the English one otherwise, and English rather than nothing where the Arabic was never
/// filled in. The same rule <c>LeaveWorkflowTransactionHandler</c> applies to its leave type and delegates,
/// written once for the handlers that came after it.
/// </summary>
public static class WorkflowDetailDisplay
{
    /// <summary>The name in the reader's language, or null when there is no name at all.</summary>
    public static string Name(string nameEn, string nameAr)
    {
        var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

        var name = isArabic && !string.IsNullOrWhiteSpace(nameAr) ? nameAr : nameEn;

        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    /// <summary>"1042 - Ahmad Al Sayed", the way the leave form shows a person.</summary>
    public static string Employee(string code, string fullNameEn, string fullNameAr)
    {
        var name = Name(fullNameEn, fullNameAr);

        if (string.IsNullOrWhiteSpace(code))
        {
            return name;
        }

        return name == null ? code : code + " - " + name;
    }
}
