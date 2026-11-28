using System;
using System.Threading.Tasks;
using Abp.Domain.Services;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>The additional unpaid leave a leave carried before it was changed, so the change can find its row.</summary>
public class AdditionalUnpaidLeaveSnapshot
{
    public int? AbsenceTypeId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public static AdditionalUnpaidLeaveSnapshot Of(EmployeeAbsence leave)
    {
        return new AdditionalUnpaidLeaveSnapshot
        {
            AbsenceTypeId = leave.ExtAbsenceTypeId,
            StartDate = leave.ExtStartDate?.Date,
            EndDate = leave.ExtEndDate?.Date
        };
    }
}

/// <summary>
/// The additional unpaid leave booked alongside a leave (<c>ExtAbsenceTypeId</c> / <c>ExtStartDate</c> /
/// <c>ExtEndDate</c>) becomes a leave row of its own -- legacy <c>EmployeeAbsence.SaveAdditionalUnpaidLeave</c>
/// (4612-4640) on HR's save and <c>AbsenceRequest.SaveAdditionalUnpaidLeave</c> (2342-2394) on approval.
/// One implementation for both, so the row, its day count and its quota charge are the same whichever door the
/// leave came through. The Ext fields stay on the main leave afterwards: they are how a later return finds the
/// row to shorten or remove.
/// </summary>
public interface IAdditionalUnpaidLeaveService : IDomainService
{
    /// <summary>
    /// Inserts the unpaid row for <paramref name="leave"/>'s Ext values and charges its quota (normally none).
    /// Does nothing when the Ext values are incomplete or a matching row already exists.
    /// </summary>
    Task<EmployeeAbsence> CreateAsync(EmployeeAbsence leave, RequestStatus? workflowStatus, int? workflowRequestId);

    /// <summary>
    /// HR edited the leave: brings its unpaid row in line with the Ext values now on it -- created when newly
    /// entered, moved when the type or dates changed, removed when cleared -- without ever creating a second one.
    /// </summary>
    Task SyncAsync(EmployeeAbsence leave, AdditionalUnpaidLeaveSnapshot previous);
}
