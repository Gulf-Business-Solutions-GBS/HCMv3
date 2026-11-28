using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IAdditionalUnpaidLeaveService"/>
[UnitOfWork]
public class AdditionalUnpaidLeaveService : HCMv3DomainServiceBase, IAdditionalUnpaidLeaveService
{
    private readonly IRepository<EmployeeAbsence> _absences;
    private readonly IEmployeeAbsenceCalculationService _calculation;
    private readonly ILeaveQuotaLedger _quotaLedger;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public AdditionalUnpaidLeaveService(
        IRepository<EmployeeAbsence> absences,
        IEmployeeAbsenceCalculationService calculation,
        ILeaveQuotaLedger quotaLedger,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _absences = absences;
        _calculation = calculation;
        _quotaLedger = quotaLedger;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<EmployeeAbsence> CreateAsync(EmployeeAbsence leave, RequestStatus? workflowStatus, int? workflowRequestId)
    {
        if (leave.ExtAbsenceTypeId == null || leave.ExtStartDate == null || leave.ExtEndDate == null)
        {
            return null;
        }

        var start = leave.ExtStartDate.Value.Date;
        var end = leave.ExtEndDate.Value.Date;

        if (Find(leave, leave.ExtAbsenceTypeId.Value, start, end) != null)
        {
            return null;
        }

        // Its days are counted with the MAIN type's week-off and holiday rules: legacy passed this.AbsenceType,
        // not the unpaid one.
        var totalLeaveDays = leave.ExtTotalLeaveDays
                             ?? await _calculation.CalculateTotalLeaveDaysAsync(leave.EmployeeId, leave.AbsenceTypeId, start, end, 0);

        var unpaid = new EmployeeAbsence
        {
            TenantId = leave.TenantId,
            EmployeeId = leave.EmployeeId,
            AbsenceTypeId = leave.ExtAbsenceTypeId.Value,
            StartDate = start,
            EndDate = end,
            InitialEndDate = end,
            CalendarDays = (end - start).TotalDays + 1,
            TotalLeaveDays = totalLeaveDays,
            Comments = leave.Comments,
            LeavePaymentType = LeavePaymentType.PayWithPayroll,
            WorkflowStatus = workflowStatus,
            WorkflowRequestId = workflowRequestId
        };

        await _absences.InsertAsync(unpaid);
        await _unitOfWorkManager.Current.SaveChangesAsync();

        // Unpaid leave is normally not quota-controlled, so this is usually a no-op -- but if the tenant does keep
        // a quota for it, it is charged like any other effective leave (legacy: the new row's own save did it).
        if (totalLeaveDays > 0)
        {
            await _quotaLedger.ConsumeAsync(unpaid.EmployeeId, unpaid.AbsenceTypeId, totalLeaveDays, unpaid.StartDate);
        }

        return unpaid;
    }

    public async Task SyncAsync(EmployeeAbsence leave, AdditionalUnpaidLeaveSnapshot previous)
    {
        var existing = previous?.AbsenceTypeId != null && previous.StartDate != null && previous.EndDate != null
            ? Find(leave, previous.AbsenceTypeId.Value, previous.StartDate.Value.Date, previous.EndDate.Value.Date)
            : null;

        var wanted = leave.ExtAbsenceTypeId != null && leave.ExtStartDate != null && leave.ExtEndDate != null;

        if (!wanted)
        {
            if (existing != null)
            {
                using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
                {
                    await _absences.DeleteAsync(existing);
                }

                await _quotaLedger.ReleaseAsync(existing.EmployeeId, existing.AbsenceTypeId, existing.TotalLeaveDays, existing.StartDate);
            }

            return;
        }

        if (existing == null)
        {
            await CreateAsync(leave, leave.WorkflowStatus, leave.WorkflowRequestId);
            return;
        }

        var start = leave.ExtStartDate.Value.Date;
        var end = leave.ExtEndDate.Value.Date;
        var totalLeaveDays = leave.ExtTotalLeaveDays
                             ?? await _calculation.CalculateTotalLeaveDaysAsync(leave.EmployeeId, leave.AbsenceTypeId, start, end, 0);

        if (existing.AbsenceTypeId == leave.ExtAbsenceTypeId.Value
            && existing.StartDate.Date == start
            && existing.EndDate.Date == end
            && Math.Abs(existing.TotalLeaveDays - totalLeaveDays) < 0.000001)
        {
            return;
        }

        var previousTypeId = existing.AbsenceTypeId;
        var previousDays = existing.TotalLeaveDays;
        var previousStart = existing.StartDate;

        existing.AbsenceTypeId = leave.ExtAbsenceTypeId.Value;
        existing.StartDate = start;
        existing.EndDate = end;
        existing.InitialEndDate = end;
        existing.CalendarDays = (end - start).TotalDays + 1;
        existing.TotalLeaveDays = totalLeaveDays;

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            await _absences.UpdateAsync(existing);
        }

        await _quotaLedger.MoveAsync(
            existing.EmployeeId, previousTypeId, previousDays, previousStart,
            existing.AbsenceTypeId, existing.TotalLeaveDays, existing.StartDate);
    }

    /// <summary>The unpaid row: same employee, type and dates, and not the main leave itself.</summary>
    private EmployeeAbsence Find(EmployeeAbsence leave, int absenceTypeId, DateTime start, DateTime end)
    {
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            return _absences.GetAll()
                .Where(a => a.Id != leave.Id
                            && a.EmployeeId == leave.EmployeeId
                            && a.AbsenceTypeId == absenceTypeId
                            && a.StartDate == start
                            && a.EndDate == end
                            && a.WorkflowStatus != RequestStatus.Cancelled)
                .OrderBy(a => a.Id)
                .FirstOrDefault();
        }
    }
}
