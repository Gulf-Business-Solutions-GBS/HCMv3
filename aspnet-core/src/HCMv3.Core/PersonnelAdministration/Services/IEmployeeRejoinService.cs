using System;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The values of one <see cref="EmployeeRejoin"/> that its side effects depend on: as it is being saved, or as it
/// was last saved (legacy's <c>prevXxx</c> fields, captured in <c>OnLoaded</c>).
/// </summary>
public class EmployeeRejoinValues
{
    public int EmployeeId { get; set; }

    /// <summary>Legacy <c>StartDate</c>.</summary>
    public DateTime RejoinDate { get; set; }

    public bool CreateNewEmployeeCode { get; set; }

    public bool CarryForwardLeaveBalance { get; set; }

    public bool CarryForwardTicketBalance { get; set; }

    public bool CarryForwardIndemnityBalance { get; set; }

    public bool CarryForwardLoanBalance { get; set; }

    public static EmployeeRejoinValues From(EmployeeRejoin rejoin)
    {
        return new EmployeeRejoinValues
        {
            EmployeeId = rejoin.EmployeeId,
            RejoinDate = rejoin.RejoinDate.Date,
            CreateNewEmployeeCode = rejoin.CreateNewEmployeeCode,
            CarryForwardLeaveBalance = rejoin.CarryForwardLeaveBalance,
            CarryForwardTicketBalance = rejoin.CarryForwardTicketBalance,
            CarryForwardIndemnityBalance = rejoin.CarryForwardIndemnityBalance,
            CarryForwardLoanBalance = rejoin.CarryForwardLoanBalance
        };
    }
}

/// <summary>
/// Legacy <c>EmployeeRejoin.OnSaving</c> / <c>OnDeleting</c>: the checks and side effects of saving or deleting a
/// rejoin. See <see cref="EmployeeRejoinService"/>.
///
/// <para>The caller runs everything in one unit of work, so a rejoin and all it writes are saved together or not
/// at all. Pass <c>previous</c> = null for a new rejoin (legacy's <c>Oid == -1</c>).</para>
/// </summary>
public interface IEmployeeRejoinService : IDomainService
{
    /// <summary>
    /// Legacy <c>PerformValidations</c>: refuses the save when the entry as it was loaded can no longer be changed,
    /// or when the new values clash with a later exit, Full &amp; Final, payroll run or rejoin.
    /// </summary>
    Task ValidateSaveAsync(EmployeeRejoinValues current, EmployeeRejoinValues previous, int? rejoinId);

    /// <summary>
    /// Legacy <c>UpdatePreviousValues</c> then <c>SaveEmployeeInformation</c>: undoes what the previous save wrote,
    /// then reactivates the employee or creates the new one, and zeroes or carries forward the balances.
    /// Returns the new employee's id when one was created.
    /// </summary>
    Task<int?> ApplyAsync(EmployeeRejoinValues current, EmployeeRejoinValues previous);

    /// <summary>Legacy <c>OnDeleting</c> -> <c>ValidatePreviousEntry</c>.</summary>
    Task ValidateDeleteAsync(EmployeeRejoinValues previous, int rejoinId);

    /// <summary>Legacy <c>OnDeleting</c> -> <c>UpdatePreviousValues</c>.</summary>
    Task UndoAsync(EmployeeRejoinValues previous);
}
