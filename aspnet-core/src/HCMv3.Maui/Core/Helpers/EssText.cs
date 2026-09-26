using System.Globalization;
using HCMv3.Workflow;

namespace HCMv3.Maui.Core.Helpers;

/// <summary>
/// Small display helpers shared by the self-service pages: picking the English or Arabic name a DTO carries,
/// formatting dates the way the rest of the product types them (dd/MM/yyyy), and the request-status badge.
/// </summary>
public static class EssText
{
    public static bool IsArabic =>
        CultureInfo.CurrentUICulture.Name.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

    /// <summary>The name in the UI language, falling back to the other one when it is empty.</summary>
    public static string Pick(string en, string ar)
    {
        if (IsArabic)
        {
            return string.IsNullOrWhiteSpace(ar) ? en ?? string.Empty : ar;
        }

        return string.IsNullOrWhiteSpace(en) ? ar ?? string.Empty : en;
    }

    /// <summary>
    /// dd/MM/yyyy, in the invariant (Gregorian) calendar so an Arabic UI does not switch to Hijri dates.
    /// An unset date (year 1, which a draft may hold) shows as a dash rather than as a real-looking value.
    /// </summary>
    public static string Date(DateTime? value)
    {
        if (!value.HasValue || value.Value.Year <= 1)
        {
            return "-";
        }

        return value.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    }

    public static string Time(DateTime? value)
    {
        if (!value.HasValue)
        {
            return "-";
        }

        return value.Value.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    public static string DateTimeText(DateTime? value)
    {
        if (!value.HasValue || value.Value.Year <= 1)
        {
            return "-";
        }

        return value.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
    }

    public static string Number(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Badge label for a workflow status. No status means the row was entered by HR rather than requested,
    /// shown as "Recorded" -- the same rule the Angular self-service screens follow.
    /// </summary>
    public static string StatusLabel(RequestStatus? status, bool isDraft = false)
    {
        if (isDraft)
        {
            return HCMv3.Maui.Core.Localization.L.Localize("WorkflowRequestStatus_" + RequestStatus.Draft);
        }

        if (!status.HasValue)
        {
            return HCMv3.Maui.Core.Localization.L.Localize("Recorded");
        }

        return HCMv3.Maui.Core.Localization.L.Localize("WorkflowRequestStatus_" + status.Value);
    }

    /// <summary>Badge CSS class for a workflow status, matching the Angular self-service screens.</summary>
    public static string StatusCss(RequestStatus? status, bool isDraft = false)
    {
        if (isDraft)
        {
            return "badge-light-warning";
        }

        switch (status)
        {
            case RequestStatus.Open:
                return "badge-warning";
            case RequestStatus.Approved:
            case RequestStatus.Closed:
                return "badge-success";
            case RequestStatus.Rejected:
                return "badge-danger";
            case RequestStatus.Draft:
                return "badge-light-warning";
            default:
                return "badge-secondary";
        }
    }
}
