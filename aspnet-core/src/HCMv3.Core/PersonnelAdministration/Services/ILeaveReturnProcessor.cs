using System;
using System.Threading.Tasks;
using Abp.Domain.Services;
using HCMv3.PersonnelAdministration.Workflow;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>An employee coming back from leave, whoever is recording it.</summary>
public class LeaveReturnInstruction
{
    /// <summary>The leave being returned from.</summary>
    public EmployeeAbsence Leave { get; set; }

    /// <summary>The first day back at work.</summary>
    public DateTime ReturnDate { get; set; }

    /// <summary>LATE return: the part of the gap to book as further paid leave of the same type.</summary>
    public DateTime? PaidStartDate { get; set; }

    /// <inheritdoc cref="PaidStartDate"/>
    public DateTime? PaidEndDate { get; set; }

    /// <summary>LATE return: the part of the gap to book as unpaid leave (type code "2").</summary>
    public DateTime? UnpaidStartDate { get; set; }

    /// <inheritdoc cref="UnpaidStartDate"/>
    public DateTime? UnpaidEndDate { get; set; }

    /// <summary>EARLY return: what happens to additional unpaid leave booked alongside this one.</summary>
    public AdditionalLeaveAction AdditionalLeaveAction { get; set; }

    /// <inheritdoc cref="AdditionalLeaveAction"/>
    public DateTime? AdditionalLeaveEndDate { get; set; }

    /// <summary>The request that approved this return, stamped on any rows it generates so the history can
    /// say where they came from. Null when HR recorded the return directly.</summary>
    public int? WorkflowRequestId { get; set; }
}

/// <summary>
/// Everything that happens when an employee comes back from leave: the late-return rules and the rows they
/// generate, the additional-unpaid-leave outcomes of an early return, and the return stamp on back-to-back
/// leave.
///
/// <para>
/// Legacy did all of this in <c>EmployeeAbsence.SaveDiffInformation</c> / <c>AbsenceRequest</c>'s approval, so
/// a return recorded by HR and one approved through the workflow behaved identically. This service is that
/// shared routine: the ESS approval calls it, and so does HR's own screen.
/// </para>
/// </summary>
public interface ILeaveReturnProcessor : IDomainService
{
    /// <summary>Legacy <c>DaysDifference</c>: positive when late, negative when early, zero when on time
    /// (including a gap made only of week-offs and holidays). Measured from the end of the additional unpaid
    /// leave when there is one.</summary>
    Task<double> GetDaysDifferenceAsync(EmployeeAbsence leave, DateTime returnDate);

    /// <summary>The late-return split rules. Throws with legacy's own refusals.</summary>
    Task ValidateLateReturnAsync(LeaveReturnInstruction instruction);

    /// <summary>
    /// Books the late gap as further leave, applies the additional-leave outcome of an early return, and marks
    /// the leave returned. The caller owns the leave row's own dates.
    /// </summary>
    Task ApplyAsync(LeaveReturnInstruction instruction);
}
