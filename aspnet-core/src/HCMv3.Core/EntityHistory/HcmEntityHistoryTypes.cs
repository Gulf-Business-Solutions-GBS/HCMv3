using System;
using HCMv3.Recruitment;

namespace HCMv3.EntityHistory;

/// <summary>
/// HCM entities whose changes ABP entity history records (AbpEntityChanges / AbpEntityPropertyChanges).
/// Kept out of the ASP.NET Zero <see cref="EntityHistoryHelper"/> so the template file only carries a single
/// hook line (<c>.Concat(HcmEntityHistoryTypes.TrackedTypes)</c>) and stays easy to upgrade.
/// </summary>
/// <remarks>
/// Add a type here only when a screen reads its history: every tracked save writes change rows.
/// <list type="bullet">
/// <item><see cref="ApplicationStatus"/>: the Status History tab of an application's page.</item>
/// </list>
/// </remarks>
public static class HcmEntityHistoryTypes
{
    public static readonly Type[] TrackedTypes =
    {
        typeof(ApplicationStatus)
    };
}
