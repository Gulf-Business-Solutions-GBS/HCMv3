using System.Threading.Tasks;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The rules an <see cref="EmployeeOnHold"/> row has to satisfy before it is written, whoever writes it.
///
/// <para>
/// Two writers exist: the On Hold tab (<c>EmployeeOnHoldsAppService</c>) and a separation that carries on-hold
/// flags (<see cref="ISeparationOnHoldService"/>). Legacy needed no shared service for this because both paths
/// ended in <c>EmployeeOnHold.OnSaving</c>, so the separation's hold was checked exactly like a hand-entered
/// one. This interface is how that stays true here; a second copy of the overlap check would drift.
/// </para>
/// </summary>
public interface IEmployeeOnHoldService
{
    /// <summary>
    /// Throws <see cref="Abp.UI.UserFriendlyException"/> when the end date is before the start date, or when
    /// another hold for the same employee overlaps this one (legacy <c>EmployeeOnHold.PerformValidations</c>,
    /// "OnHold Conflict found for Employee").
    ///
    /// <para>
    /// The overlap query reads the database, so a hold deleted earlier in the same unit of work must have been
    /// saved first or it will still count as a conflict.
    /// </para>
    /// </summary>
    Task ValidateAsync(EmployeeOnHold entity);
}
