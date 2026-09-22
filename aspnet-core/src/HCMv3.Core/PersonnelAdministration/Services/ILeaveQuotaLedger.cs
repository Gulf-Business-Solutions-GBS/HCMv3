using System;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The one place <c>EmployeeAbsenceQuota.QuotaUsed</c> moves. Legacy did this inside
/// <c>EmployeeAbsence.UpdateQuotaInformation</c> and <c>OnDeleting</c>, so every door -- HR's screen, an import,
/// an approval -- charged and refunded the same way. HCMv3 has more doors, not fewer, so the rule lives here and
/// each door calls it.
///
/// <para>
/// A leave awaiting approval does NOT move the quota: it consumes BOOKABLE, which is computed. Only an effective
/// leave (approved, or one HR entered directly) is charged.
/// </para>
/// </summary>
public interface ILeaveQuotaLedger : IDomainService
{
    /// <summary>Charges days to the quota row covering <paramref name="asOf"/>. Silent when the employee has no
    /// row for that type: those types are not quota-controlled.</summary>
    Task ConsumeAsync(int employeeId, int absenceTypeId, double days, DateTime asOf);

    /// <summary>Gives days back, floored at zero.</summary>
    Task ReleaseAsync(int employeeId, int absenceTypeId, double days, DateTime asOf);

    /// <summary>
    /// What an edit does: release what the leave used to take and charge what it takes now. Two steps rather
    /// than a delta, because a change of absence type moves days between two different quota rows.
    /// </summary>
    Task MoveAsync(
        int employeeId,
        int previousAbsenceTypeId,
        double previousDays,
        DateTime previousAsOf,
        int newAbsenceTypeId,
        double newDays,
        DateTime newAsOf);
}
