using System;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The one place an Indemnity record (<see cref="EmployeeGratuity"/>) moves
/// <c>EmployeeGratuityQuota.QuotaUsed</c>. Ported from legacy XAF <c>EmployeeGratuity.UpdateQuotaBalance</c>
/// (called from <c>OnSaving</c>) and <c>OnDeleting</c>. Every door that writes an Indemnity record -- the
/// Indemnity screen, Employee Rejoin and Full &amp; Final Settlement -- calls this, as every legacy door did by
/// calling <c>Save()</c>/<c>Delete()</c>.
/// </summary>
public interface IEmployeeGratuityQuotaService : IDomainService
{
    /// <summary>
    /// Legacy <c>PerformValidations</c> + <c>UpdateQuotaBalance</c>: gives the stored days back (when
    /// <paramref name="previous"/> is given, i.e. an edit), then charges the record's current days, refusing them
    /// when they are more than the quota balance. Call before the record is inserted/updated.
    /// </summary>
    Task ApplyAsync(EmployeeGratuity gratuity, EmployeeGratuitySnapshot previous);

    /// <summary>Legacy <c>OnDeleting</c>: gives the stored days back to the quota.</summary>
    Task ReverseAsync(EmployeeGratuity gratuity);
}

/// <summary>
/// Legacy's non-persistent <c>prevEmployee</c>/<c>prevGratuityPaidDate</c>/<c>prevGratuityPaidDays</c>,
/// captured in <c>OnLoaded</c> before the row is overwritten.
/// </summary>
public sealed class EmployeeGratuitySnapshot
{
    public int EmployeeId { get; init; }
    public DateTime GratuityPaidDate { get; init; }
    public double GratuityPaidDays { get; init; }

    public static EmployeeGratuitySnapshot Of(EmployeeGratuity entity) => new()
    {
        EmployeeId = entity.EmployeeId,
        GratuityPaidDate = entity.GratuityPaidDate,
        GratuityPaidDays = entity.GratuityPaidDays
    };
}
