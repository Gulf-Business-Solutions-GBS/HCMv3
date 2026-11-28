using System;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The one place a leave adjustment (<see cref="EmployeeAbsAdjustment"/>) moves
/// <c>EmployeeAbsenceQuota.QuotaAdjusted</c>. Ported from legacy XAF <c>EmployeeAbsAdjustment.UpdateQuotaBalance</c>
/// (called from <c>OnSaving</c>) and <c>OnDeleting</c>. Every door that writes an adjustment -- the Leave Abs
/// Adjustment screen and a leave's fractional-balance settlement -- calls this, as every legacy door did by
/// calling <c>Save()</c>.
/// </summary>
public interface IEmployeeAbsAdjustmentQuotaService : IDomainService
{
    /// <summary>
    /// Legacy <c>UpdateQuotaBalance</c>: gives the stored value back (when <paramref name="previous"/> is given,
    /// i.e. an edit), then charges the adjustment's current value, refusing it when it is more than the balance
    /// plus the negative-balance limit. Call before the adjustment is inserted/updated.
    /// </summary>
    Task ApplyAsync(EmployeeAbsAdjustment adjustment, EmployeeAbsAdjustmentSnapshot previous);

    /// <summary>Legacy <c>OnDeleting</c>: gives the stored value back to the quota.</summary>
    Task ReverseAsync(EmployeeAbsAdjustment adjustment);
}

/// <summary>
/// Legacy's non-persistent <c>prevEmployee</c>/<c>prevAbsenceType</c>/<c>prevAdjustmentType</c>/
/// <c>prevEffectiveDate</c>/<c>prevQuotaAdjusted</c>, captured in <c>OnLoaded</c> before the row is overwritten.
/// </summary>
public sealed class EmployeeAbsAdjustmentSnapshot
{
    public int EmployeeId { get; init; }
    public int AbsenceTypeId { get; init; }
    public AdjustmentType AdjustmentType { get; init; }
    public DateTime EffectiveDate { get; init; }
    public double QuotaAdjusted { get; init; }

    public static EmployeeAbsAdjustmentSnapshot Of(EmployeeAbsAdjustment entity) => new()
    {
        EmployeeId = entity.EmployeeId,
        AbsenceTypeId = entity.AbsenceTypeId,
        AdjustmentType = entity.AdjustmentType,
        EffectiveDate = entity.EffectiveDate,
        QuotaAdjusted = entity.QuotaAdjusted
    };
}
