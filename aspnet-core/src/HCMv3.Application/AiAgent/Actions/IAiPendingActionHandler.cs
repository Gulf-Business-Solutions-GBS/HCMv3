using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using HCMv3.AiAgent.Dto;

namespace HCMv3.AiAgent.Actions;

/// <summary>
/// Carries out one kind of <see cref="AiPendingAction"/> once the user has pressed Confirm, and describes it
/// for the confirmation card before they do.
///
/// <para>
/// This is the ONLY code path by which the assistant changes anything, and it only ever runs from the user's
/// click. A handler calls the same self-service application service the ordinary screen calls, so approval
/// routing, balance and overlap checks, and permissions are identical to applying by hand.
/// </para>
///
/// <para>
/// Name implementations <c>{X}AiPendingActionHandler</c>: Castle binds by name suffix, and a handler named
/// otherwise is silently unreachable. <c>AiAgentPendingActions_Tests</c> guards this.
/// </para>
/// </summary>
public interface IAiPendingActionHandler : ITransientDependency
{
    /// <summary>Stored on <see cref="AiPendingAction.ActionType"/>. Never change a released value.</summary>
    string ActionType { get; }

    /// <summary>Title and field/value lines for the card, in the current UI language.</summary>
    AiPendingActionPreview Describe(string payloadJson);

    /// <summary>
    /// Does it. Returns the message to show the user. Throws <c>UserFriendlyException</c> when the underlying
    /// service refuses -- the refusal is recorded against the action and shown, not swallowed.
    /// </summary>
    Task<string> ExecuteAsync(string payloadJson);
}

public class AiPendingActionPreview
{
    public string Title { get; set; }

    public List<AiActionDetailDto> Details { get; set; } = new();

    public AiPendingActionPreview Add(string label, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            Details.Add(new AiActionDetailDto { Label = label, Value = value });
        }

        return this;
    }
}

public abstract class AiPendingActionHandlerBase : AbpServiceBase, IAiPendingActionHandler
{
    private static readonly CultureInfo Kuwait = CultureInfo.GetCultureInfo("ar-KW");

    protected AiPendingActionHandlerBase()
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
    }

    public abstract string ActionType { get; }

    public abstract AiPendingActionPreview Describe(string payloadJson);

    public abstract Task<string> ExecuteAsync(string payloadJson);

    public static string Serialize(object payload) => JsonSerializer.Serialize(payload);

    protected static T Read<T>(string payloadJson) =>
        JsonSerializer.Deserialize<T>(payloadJson) ?? throw new InvalidOperationException("Empty AI action payload.");

    protected static bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

    protected static string PickName(string nameEn, string nameAr) =>
        IsArabic && !string.IsNullOrWhiteSpace(nameAr) ? nameAr : nameEn;

    /// <summary>
    /// ar-KW rather than plain "ar": the neutral Arabic culture defaults to the Hijri calendar, and a leave
    /// card showing Hijri dates for a Gregorian request would read as the wrong dates.
    /// </summary>
    protected static string FormatDate(DateTime date) =>
        IsArabic
            ? date.ToString("d MMMM yyyy", Kuwait)
            : date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    protected static string FormatNumber(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
