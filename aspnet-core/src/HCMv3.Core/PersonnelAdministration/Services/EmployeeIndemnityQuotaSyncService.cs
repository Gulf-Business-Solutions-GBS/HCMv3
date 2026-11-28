using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Ported from legacy XAF <c>Employee.OnSaving</c> ("Update Indemnity StartDate") and
/// <c>EmployeeSeparation.SaveOtherInfo</c> ("Advance EndOfService"). See <see cref="IEmployeeIndemnityQuotaSyncService"/>.
/// </summary>
public class EmployeeIndemnityQuotaSyncService : HCMv3DomainServiceBase, IEmployeeIndemnityQuotaSyncService
{
    private readonly IRepository<EmployeeGratuityQuota> _quotaRepository;
    private readonly IRepository<SeparationReason> _separationReasonRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IAccrualService _accrualService;

    public EmployeeIndemnityQuotaSyncService(
        IRepository<EmployeeGratuityQuota> quotaRepository,
        IRepository<SeparationReason> separationReasonRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IAccrualService accrualService)
    {
        _quotaRepository = quotaRepository;
        _separationReasonRepository = separationReasonRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _accrualService = accrualService;
    }

    public async Task AlignToJoiningDateAsync(int employeeId, DateTime joiningDate)
    {
        // Legacy: XPQuery<EmployeeGratuityQuota>.Where(Employee).OrderBy(EntitlmentFrom).FirstOrDefault()
        var quota = _quotaRepository.GetAll()
            .Where(q => q.EmployeeId == employeeId)
            .OrderBy(q => q.EntitlementFrom)
            .ThenBy(q => q.Id)
            .FirstOrDefault();

        if (quota == null)
        {
            return;
        }

        quota.EntitlementFrom = joiningDate.Date;
        quota.DeductionFrom = joiningDate.Date;
        await _quotaRepository.UpdateAsync(quota);
    }

    public async Task<bool> ApplyAdvanceEndOfServiceAsync(int employeeId, int separationReasonId, DateTime lastDayInService)
    {
        var reason = await _separationReasonRepository.FirstOrDefaultAsync(separationReasonId);
        if (reason == null || !reason.AllowAdvanceEoS)
        {
            return false;
        }

        // Legacy: this.Employee.IndemnityQuota.FirstOrDefault() -- the collection's natural (key) order.
        var quota = _quotaRepository.GetAll()
            .Where(q => q.EmployeeId == employeeId)
            .OrderBy(q => q.Id)
            .FirstOrDefault();

        if (quota == null)
        {
            return false;
        }

        // Legacy AccrualService.GetTotalIndemnityDays reads the employee group (max-indemnity cap) and sub group
        // (slab table) from the org assignment in force on the as-on date.
        var date = lastDayInService.Date;
        var orgAssignment = _orgAssignmentRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.StartDate <= date && r.EndDate >= date)
            .Select(r => new { r.EmployeeGroupId, r.EmployeeSubGroupId })
            .FirstOrDefault();

        quota.QuotaAccrued = await _accrualService.GetTotalIndemnityDaysAsync(
            employeeId,
            lastDayInService,
            orgAssignment?.EmployeeGroupId,
            orgAssignment?.EmployeeSubGroupId,
            indemnityPayRuleId: null);

        await _quotaRepository.UpdateAsync(quota);
        return true;
    }
}
