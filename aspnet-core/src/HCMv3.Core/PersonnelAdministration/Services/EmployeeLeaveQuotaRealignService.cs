using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using HCMv3.PayrollManagement;
using HCMv3.SystemSettings.Services;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>See <see cref="IEmployeeLeaveQuotaRealignService"/>.</summary>
public class EmployeeLeaveQuotaRealignService : HCMv3DomainServiceBase, IEmployeeLeaveQuotaRealignService
{
    /// <summary>Legacy annual leave: <c>AbsenceType.Code == "1"</c>.</summary>
    public const string AnnualLeaveCode = "1";

    private readonly IRepository<EmployeeAbsenceQuota> _quotaRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public EmployeeLeaveQuotaRealignService(
        IRepository<EmployeeAbsenceQuota> quotaRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        ISysSettingsAccessor sysSettingsAccessor)
    {
        _quotaRepository = quotaRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    public async Task RealignAsync(int employeeId, DateTime joiningDate)
    {
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        var deductionStartMonth = timeSettings?.LeaveDeductionStartMonth ?? 0;

        var leaveTypes = await _absenceTypeRepository.GetAll()
            .Where(t => t.HaveQuotaAccrual && t.IsActive)
            .ToListAsync();

        foreach (var leaveType in leaveTypes)
        {
            var quota = await _quotaRepository.GetAll()
                .Where(q => q.EmployeeId == employeeId && q.AbsenceTypeId == leaveType.Id)
                .OrderBy(q => q.EntitlementFrom)
                .FirstOrDefaultAsync();

            // Legacy only changes WorkYear, CalendarYear, FiscalYear and Custom leave types.
            if (quota == null || !IsRealigned(leaveType.AccrualPeriod))
            {
                continue;
            }

            quota.EntitlementFrom = GetEntitlementFrom(leaveType, joiningDate);
            // Annual leave can only be taken LeaveDeductionStartMonth months after entitlement starts.
            quota.DeductionFrom = leaveType.Code == AnnualLeaveCode
                ? quota.EntitlementFrom.AddMonths(deductionStartMonth)
                : quota.EntitlementFrom;

            await _quotaRepository.UpdateAsync(quota);
        }
    }

    public DateTime GetEntitlementFrom(AbsenceType leaveType, DateTime joiningDate)
    {
        switch (leaveType.AccrualPeriod)
        {
            case AccrualPeriod.FiscalYear:
                return new DateTime(joiningDate.Year, 4, 1);
            case AccrualPeriod.Custom:
                return leaveType.CustomStartMonth == CustomStartMonth.None
                    ? joiningDate.Date
                    : new DateTime(joiningDate.Year, (int)leaveType.CustomStartMonth, 1);
            default:
                // WorkYear and CalendarYear: legacy starts both from the joining date itself.
                return joiningDate.Date;
        }
    }

    private static bool IsRealigned(AccrualPeriod period) =>
        period is AccrualPeriod.WorkYear or AccrualPeriod.CalendarYear or AccrualPeriod.FiscalYear or AccrualPeriod.Custom;
}
