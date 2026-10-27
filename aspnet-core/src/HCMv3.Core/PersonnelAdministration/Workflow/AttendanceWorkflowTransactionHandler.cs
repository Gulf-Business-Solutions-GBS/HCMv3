using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// Workflow handler for attendance corrections (<see cref="EmployeeAttendance"/>).
///
/// <para>
/// This is the type where the ApprovedOnly filter earns its keep most visibly. An attendance row feeds the
/// Time Engine and then payroll, so a correction that took effect the moment it was typed would change
/// somebody's pay before anyone agreed to it. Pending corrections sit in the same table as approved ones and
/// are simply invisible until granted.
/// </para>
/// </summary>
public class AttendanceWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeAttendance>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for attendance.</summary>
    public const string AttendanceRequestType = "EmployeeAttendance";

    private readonly IRepository<AttendanceType> _attendanceTypeRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly Services.IEmployeeAttendanceCalculationService _attendanceCalculation;

    public AttendanceWorkflowTransactionHandler(
        IRepository<EmployeeAttendance> repository,
        IRepository<AttendanceType> attendanceTypeRepository,
        IUnitOfWorkManager unitOfWorkManager,
        Services.IEmployeeAttendanceCalculationService attendanceCalculation)
        : base(repository, unitOfWorkManager)
    {
        _attendanceTypeRepository = attendanceTypeRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _attendanceCalculation = attendanceCalculation;
    }

    public override string RequestType => AttendanceRequestType;

    /// <summary>
    /// The moment a requested attendance becomes real. Legacy created the <c>EmployeeAttendance</c> row only now,
    /// and that row's <c>OnSaving</c> worked out <c>TotalAttendDays</c> and accrued it into the linked leave quota
    /// (<c>EmployeeAttendance.UpdateAttendanceQuota</c>, lines 348-436) -- so the same happens here. A row HR keyed
    /// in directly was already accrued by the admin screen and never reaches this point.
    /// </summary>
    public override async Task OnApprovedAsync(WorkflowTransactionContext context)
    {
        await base.OnApprovedAsync(context);

        if (context.IsAmendment)
        {
            return;
        }

        var attendance = await GetTargetAsync(context);
        if (attendance == null)
        {
            return;
        }

        // Day keeps the value it was saved with (legacy lines 368-371); Period/Hours are derived.
        if (attendance.AttendanceUnit != AttendanceUnit.Day)
        {
            await _attendanceCalculation.ApplyTotalAttendDaysAsync(attendance);

            using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
            {
                await Repository.UpdateAsync(attendance);
            }
        }

        await _attendanceCalculation.AccrueAttendanceQuotaAsync(attendance);
    }

    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var attendance = await GetTargetAsync(context);
        if (attendance == null)
        {
            return;
        }

        if (attendance.EndDate.Date < attendance.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        // Only meaningful within a single day; across days the times belong to different dates and the
        // comparison would refuse legitimate overnight shifts.
        if (attendance.StartDate.Date == attendance.EndDate.Date
            && attendance.EndTime.TimeOfDay < attendance.StartTime.TimeOfDay)
        {
            throw new UserFriendlyException(L("EndTimeCannotBeEarlierThanStartTime"));
        }

        // In-flight rows must be visible for this to mean anything: without the opt-out an employee could
        // file the same correction repeatedly, each passing its own check.
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var overlapping = Repository.GetAll()
                .Any(a => a.Id != attendance.Id
                          && a.EmployeeId == attendance.EmployeeId
                          && a.AttendanceTypeId == attendance.AttendanceTypeId
                          && a.StartDate <= attendance.EndDate
                          && a.EndDate >= attendance.StartDate
                          && (a.WorkflowStatus == null
                              || a.WorkflowStatus == RequestStatus.Approved
                              || a.WorkflowStatus == RequestStatus.Closed
                              || a.WorkflowStatus == RequestStatus.Open));

            if (overlapping)
            {
                throw new UserFriendlyException(L("OverlappingAttendanceRequestExists"));
            }
        }
    }

    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var attendance = await GetTargetAsync(context);
        if (attendance == null)
        {
            return null;
        }

        var type = _attendanceTypeRepository.GetAll()
            .FirstOrDefault(t => t.Id == attendance.AttendanceTypeId);

        var range = attendance.StartDate.ToString("dd MMM yyyy");
        if (attendance.EndDate.Date != attendance.StartDate.Date)
        {
            range += " - " + attendance.EndDate.ToString("dd MMM yyyy");
        }

        var hours = attendance.AttendanceHours > 0
            ? $", {attendance.AttendanceHours:0.##} hour(s)"
            : string.Empty;

        return new WorkflowSummary(
            $"{type?.NameEn ?? "Attendance"}, {range}{hours}",
            $"{type?.NameAr ?? "الحضور"}، {range}{hours}");
    }
}
