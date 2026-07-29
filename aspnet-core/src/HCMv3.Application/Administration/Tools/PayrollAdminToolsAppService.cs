using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.Administration.Tools.Dto;
using HCMv3.Authorization;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.Administration.Tools;

/// <inheritdoc cref="IPayrollAdminToolsAppService"/>
/// <remarks>
/// <para><b>DELIBERATE DEVIATION — permission instead of a username check.</b> Legacy gated the entire
/// panel with <c>CheckIfSuperAdminUser()</c> (uscPayrollConfigPanel.ascx.cs line 60), comparing the logged-in
/// username against the literal <c>SysService.userSuperAdmin</c>. Hardcoded-username authorization does not
/// survive multi-tenancy; each operation now carries its own permission.</para>
///
/// <para><b>DELIBERATE DEVIATION — lookups by Id, not by display name.</b> Legacy matched PayrollArea by the
/// combo's display <c>Name</c>. <c>PayrollArea.Name</c> is not unique, so this port takes ids.</para>
///
/// <para><b>DELIBERATE DEVIATION — no per-row commit.</b> Legacy called <c>object_Space.CommitChanges()</c>
/// inside its loops. ABP wraps each app-service method in one unit of work, so the whole tool is atomic
/// here. This is strictly safer for the destructive tools: legacy could half-delete a period and then throw.</para>
///
/// <para><b>NOT PORTED — the four "Fix ..." retro-recalculation panels.</b> See
/// <see cref="FixAnnualLeaveProvision"/>.</para>
/// </remarks>
[AbpAuthorize(AppPermissions.Pages_Administration_Tools)]
public class PayrollAdminToolsAppService : HCMv3AppServiceBase, IPayrollAdminToolsAppService
{
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<PayrollCalendar> _payrollCalendarRepository;
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeAbsenceQuota> _employeeAbsenceQuotaRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IAccrualService _accrualService;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public PayrollAdminToolsAppService(
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<PayrollCalendar> payrollCalendarRepository,
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeAbsenceQuota> employeeAbsenceQuotaRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IAccrualService accrualService,
        ISysSettingsAccessor sysSettingsAccessor)
    {
        _payrollAreaRepository = payrollAreaRepository;
        _payrollCalendarRepository = payrollCalendarRepository;
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _employeeRepository = employeeRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeAbsenceQuotaRepository = employeeAbsenceQuotaRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _accrualService = accrualService;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    #region Generate Payroll Calendar (legacy line 98)

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_Administration_Tools_GeneratePayrollCalendar)]
    public async Task<AdminToolResultDto> GeneratePayrollCalendar(GeneratePayrollCalendarInput input)
    {
        var result = new AdminToolResultDto();

        var payrollArea = _payrollAreaRepository.GetAll()
            .FirstOrDefault(r => r.Id == input.PayrollAreaId);
        if (payrollArea == null)
        {
            throw new UserFriendlyException(L("PayrollAreaNotFound"));
        }

        if (input.EndDate < input.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeBeforeStartDate"));
        }

        // Legacy PayrollControlRecord.GetStartEndDateFromPayrollPeriod (PayrollControlRecord.cs line 542)
        // silently returned a pair of null-dates when the area had NO PayrollControlRecord at all, which
        // legacy then happily saved onto the new calendar row. That is a data-corruption bug; this port
        // fails loudly instead.
        var hasControlRecord = _payrollControlRecordRepository.GetAll()
            .Any(r => r.PayrollAreaId == payrollArea.Id);
        if (!hasControlRecord)
        {
            throw new UserFriendlyException(L("PayrollControlRecordNotFoundForPayrollArea"));
        }

        var payrollPeriodCode = _payrollAreaRepository.GetAll()
            .Where(r => r.Id == payrollArea.Id)
            .Select(r => r.PayrollPeriod.Code)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(payrollPeriodCode))
        {
            throw new UserFriendlyException(L("IncorrectPayrollPeriod"));
        }

        var existing = _payrollCalendarRepository.GetAll()
            .Where(r => r.PayrollAreaId == payrollArea.Id)
            .Select(r => new { r.PayrollMonth, r.PayrollYear })
            .ToList();

        // Legacy line 110: the start date is normalised to the 1st of its month, and the loop is inclusive
        // of the end date's month.
        var date = new DateTime(input.StartDate.Year, input.StartDate.Month, 1);
        var endDate = input.EndDate;

        while (date.Year < endDate.Year || (date.Year == endDate.Year && date.Month <= endDate.Month))
        {
            var alreadyExists = existing.Any(r => r.PayrollMonth == date.Month && r.PayrollYear == date.Year);
            if (alreadyExists)
            {
                result.SkippedCount++;
            }
            else
            {
                var (calendarMonthStart, calendarMonthEnd) =
                    PayrollPeriodShiftCalculator.GetCalendarMonthRange(payrollPeriodCode, date.Month, date.Year);

                await _payrollCalendarRepository.InsertAsync(new PayrollCalendar
                {
                    TenantId = AbpSession.GetTenantId(),
                    PayrollAreaId = payrollArea.Id,
                    PayrollMonth = date.Month,
                    PayrollYear = date.Year,
                    StartDate = calendarMonthStart,
                    EndDate = calendarMonthEnd
                });

                result.AffectedCount++;
                result.Messages.Add($"{date.Month:00}/{date.Year}");
            }

            date = date.AddMonths(1);
        }

        return result;
    }

    #endregion

    #region Delete / Unlock Salary Register (legacy lines 1253 and 1397)

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_Administration_Tools_DeleteSalaryRegister)]
    public async Task<AdminToolResultDto> DeleteSalaryRegisters(SalaryRegisterMaintenanceInput input)
    {
        var result = new AdminToolResultDto();
        var (inPeriod, employees) = ResolveMaintenanceScope(input);

        foreach (var employee in employees)
        {
            GuardNoLaterRegister(employee, inPeriod, input.PayrollRunType);

            var registers = _salaryRegisterRepository.GetAll()
                .Where(r => r.EmployeeId == employee.Id
                            && r.PayrollRunType == input.PayrollRunType
                            && r.InPeriod == inPeriod)
                .ToList();

            if (registers.Count == 0)
            {
                continue;
            }

            var registerIds = registers.Select(r => r.Id).ToList();

            // Legacy deleted rec.SalaryRegisterDetail then rec (lines 1355-1358). Details are deleted first
            // here too so the FK never dangles even mid-transaction.
            var details = _salaryRegisterDetailRepository.GetAll()
                .Where(r => registerIds.Contains(r.SalaryRegisterId))
                .ToList();

            foreach (var detail in details)
            {
                await _salaryRegisterDetailRepository.DeleteAsync(detail.Id);
            }

            foreach (var register in registers)
            {
                await _salaryRegisterRepository.DeleteAsync(register.Id);
            }

            result.AffectedCount += registers.Count;
            result.Messages.Add($"{employee.Code}: {registers.Count}");
        }

        return result;
    }

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_Administration_Tools_UnlockSalaryRegister)]
    public async Task<AdminToolResultDto> UnlockSalaryRegisters(SalaryRegisterMaintenanceInput input)
    {
        var result = new AdminToolResultDto();
        var (inPeriod, employees) = ResolveMaintenanceScope(input);

        foreach (var employee in employees)
        {
            GuardNoLaterRegister(employee, inPeriod, input.PayrollRunType);

            // DEVIATION: legacy filtered on `Locked == 1` because legacy's Locked column is an int. In this
            // port SalaryRegister.Locked is a bool, so the filter is `Locked` — same set of rows.
            var locked = _salaryRegisterRepository.GetAll()
                .Where(r => r.EmployeeId == employee.Id
                            && r.PayrollRunType == input.PayrollRunType
                            && r.InPeriod == inPeriod
                            && r.Locked)
                .ToList();

            foreach (var register in locked)
            {
                register.Locked = false;
                await _salaryRegisterRepository.UpdateAsync(register);
            }

            if (locked.Count > 0)
            {
                result.AffectedCount += locked.Count;
                result.Messages.Add($"{employee.Code}: {locked.Count}");
            }
        }

        return result;
    }

    /// <summary>
    /// Shared translation of the legacy filter block that both the delete (lines 1275-1307) and unlock
    /// (lines 1419-1453) panels duplicated verbatim.
    /// </summary>
    private (DateTime InPeriod, List<Employee> Employees) ResolveMaintenanceScope(SalaryRegisterMaintenanceInput input)
    {
        var hasEmployeeCodes = input.EmployeeCodes != null && input.EmployeeCodes.Length > 0;

        // Legacy lines 1267-1270: both filters blank is rejected, because that would hit every employee.
        if (!input.PayrollAreaId.HasValue && !hasEmployeeCodes)
        {
            throw new UserFriendlyException(L("PayrollAreaAndEmployeeCodeBothCannotBeBlank"));
        }

        // Legacy line 1278: InPeriod is the LAST day of the requested month.
        var inPeriod = new DateTime(input.Year, input.Month, DateTime.DaysInMonth(input.Year, input.Month)).Date;

        // Legacy lines 1282-1287: employee codes override the "all active" default entirely — note it drops
        // the Status==Active filter when explicit codes are supplied. Ported faithfully.
        var query = hasEmployeeCodes
            ? _employeeRepository.GetAll().Where(r => input.EmployeeCodes.Contains(r.Code))
            : _employeeRepository.GetAll().Where(r => r.Status == EmployeeStatus.Active);

        if (input.PayrollAreaId.HasValue)
        {
            var employeeIdsInArea = _employeeOrgAssignmentRepository.GetAll()
                .Where(r => r.StartDate <= inPeriod
                            && r.EndDate >= inPeriod
                            && r.PayrollAreaId == input.PayrollAreaId.Value)
                .Select(r => r.EmployeeId);

            query = query.Where(r => employeeIdsInArea.Contains(r.Id));
        }

        return (inPeriod, query.OrderBy(r => r.Code).ToList());
    }

    /// <summary>
    /// Legacy lines 1311-1342 / 1457-1490. If a later register exists the whole operation is refused,
    /// because deleting/unlocking an earlier period would leave that later register's carried-forward
    /// balances wrong.
    /// </summary>
    private void GuardNoLaterRegister(Employee employee, DateTime inPeriod, PayrollRunType runType)
    {
        SalaryRegister blocker = null;

        if (runType == PayrollRunType.Regular)
        {
            // Legacy deliberately does NOT filter by run type here (its own filter is commented out at
            // line 1316) — ANY later register blocks.
            blocker = _salaryRegisterRepository.GetAll()
                .FirstOrDefault(r => r.EmployeeId == employee.Id && r.InPeriod > inPeriod);
        }
        else if (runType == PayrollRunType.OffCycle)
        {
            blocker = _salaryRegisterRepository.GetAll()
                .FirstOrDefault(r => r.EmployeeId == employee.Id
                                     && ((r.PayrollRunType == PayrollRunType.Regular && r.InPeriod == inPeriod)
                                         || r.InPeriod > inPeriod));
        }

        if (blocker != null)
        {
            throw new UserFriendlyException(L(
                "CannotMaintainSalaryRegisterLaterRegisterExists",
                employee.Code,
                blocker.PayrollRunType.ToString(),
                blocker.InPeriod.ToString("dd/MM/yyyy")));
        }
    }

    #endregion

    #region Fix Forfeit Quota (legacy line 1171)

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_Administration_Tools_FixForfeitQuota)]
    public async Task<AdminToolResultDto> FixForfeitQuota(FixForfeitQuotaInput input)
    {
        var result = new AdminToolResultDto();

        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        if (timeSettings == null)
        {
            throw new UserFriendlyException(L("TimeSettingsNotConfigured"));
        }

        // Legacy line 1203: the whole tool is a no-op unless the tenant forfeits monthly.
        if (timeSettings.ForfeitRule != ForfeitRule.Monthly)
        {
            result.Messages.Add(L("ForfeitRuleIsNotMonthlyNothingToDo"));
            return result;
        }

        var annualLeave = _absenceTypeRepository.GetAll().FirstOrDefault(r => r.Code == "1");
        if (annualLeave == null)
        {
            throw new UserFriendlyException(L("AnnualLeaveAbsenceTypeNotFound"));
        }

        // Legacy lines 1198-1200: clamp the requested start up to EarliestEvaluationDate.
        var forfeitQuotaStartDate = input.StartDate ?? default;
        if (forfeitQuotaStartDate == default || forfeitQuotaStartDate < timeSettings.EarliestEvaluationDate)
        {
            forfeitQuotaStartDate = timeSettings.EarliestEvaluationDate;
        }

        var hasEmployeeCodes = input.EmployeeCodes != null && input.EmployeeCodes.Length > 0;
        var employees = (hasEmployeeCodes
                ? _employeeRepository.GetAll().Where(r => input.EmployeeCodes.Contains(r.Code))
                : _employeeRepository.GetAll().Where(r => r.Status == EmployeeStatus.Active))
            .OrderBy(r => r.Code)
            .ToList();

        foreach (var employee in employees)
        {
            // Legacy line 1207: employees with no joining date are skipped entirely.
            if (employee.JoiningDate == default)
            {
                result.SkippedCount++;
                continue;
            }

            var startDate = forfeitQuotaStartDate;
            if (startDate < employee.JoiningDate)
            {
                startDate = employee.JoiningDate;
            }

            // Legacy line 1213: only employees with an annual-leave quota row spanning startDate qualify.
            var hasQuota = _employeeAbsenceQuotaRepository.GetAll()
                .Any(r => r.EmployeeId == employee.Id
                          && r.AbsenceTypeId == annualLeave.Id
                          && r.EntitlementFrom <= startDate
                          && r.EntitlementTo >= startDate);

            if (!hasQuota)
            {
                result.SkippedCount++;
                continue;
            }

            // DEVIATION: legacy passed the located EmployeeAbsenceQuota straight into
            // AccrualService.CalculateForfeitForLeave. This port's IAccrualService resolves the quota row
            // itself and instead needs the employee's sub-group, so it is resolved here from the org
            // assignment effective at startDate.
            var employeeSubGroupId = _employeeOrgAssignmentRepository.GetAll()
                .Where(r => r.EmployeeId == employee.Id && r.StartDate <= startDate && r.EndDate >= startDate)
                .Select(r => r.EmployeeSubGroupId)
                .FirstOrDefault();

            // Legacy passed (startDate, startDate) as the window and null for the previous absence.
            var forfeitResult = await _accrualService.CalculateForfeitForLeaveAsync(
                employee.Id, employeeSubGroupId, startDate, startDate, prevEmployeeAbsenceId: null);

            result.AffectedCount++;
            result.Messages.Add($"{employee.Code}: {forfeitResult.TotalForfeitDays}");
        }

        return result;
    }

    #endregion

    #region NOT PORTED — the four retro-recalculation panels

    /// <summary>
    /// <b>NOT PORTED.</b> Legacy uscPayrollConfigPanel "Fix Annual Leave Provision"
    /// (<c>ASPxCallbackPanel2_Callback</c>, line 143), "Fix Indemnity Provision"
    /// (<c>ASPxCallbackPanel5_Callback</c>, line 564), "Fix Total Leave Days"
    /// (<c>ASPxCallbackPanel4_Callback</c>, line 922) and "Fix Absent Unpaid Days"
    /// (<c>ASPxCallbackPanel6_Callback</c>, line 999).
    ///
    /// <para>Together these are ~900 lines that reach directly into the legacy payroll engine's mutable
    /// static state — <c>PayrollEngine.Program.GetInitialVariables</c>, then hand-assigning
    /// <c>Program.empTimeTransferList</c>, <c>empIndemnityList</c>, <c>empSalaryRegister</c>,
    /// <c>empContractList</c>, <c>empOrgAssignmentList</c>, <c>empAbsQuotaForfeitList</c>,
    /// <c>empTimeRegisterList</c>, <c>prevSalRegList</c>, <c>currentSalRegisters</c> per employee/month —
    /// and then re-invoke individual wage-type calculations for ~24 hardcoded wage type codes (4000, 5000,
    /// 4021, 5021, 4010, 5010, 4030, 5030, 4031, 5031, 4022, 5022, 3001, 6001, 3004, 6004, 4025, 4004,
    /// 5004, 4005, 4026, 4006, 3000, 6000, 3003, 6003, 4001, 5001, 4024, 5024).</para>
    ///
    /// <para>This port's engine is a set of injected, stateless domain services
    /// (<c>HCMv3.Core/PayrollManagement/Services/*WageTypeCalculator.cs</c>,
    /// <c>AnnualLeaveProvisionService</c>, <c>IIndemnityCalculationService</c>) with no static context to
    /// prime, so there is no faithful line-by-line translation available — a correct rebuild is a
    /// "recalculate a historical period through the payroll run orchestrator" feature that must be designed
    /// against the ported engine and validated against production payroll figures. Shipping a half-ported
    /// version of a tool that silently rewrites historical salary registers would be worse than shipping
    /// nothing.</para>
    ///
    /// <para><b>Reported as an explicit gap, not silently skipped.</b></para>
    /// </summary>
    /// <exception cref="NotImplementedException">Always.</exception>
    [AbpAuthorize(AppPermissions.Pages_Administration_Tools_FixPayrollProvisions)]
    public Task<AdminToolResultDto> FixAnnualLeaveProvision()
    {
        throw new NotImplementedException(
            "NOT PORTED. Legacy source: HCM10.Web/UserControls/Administration/uscPayrollConfigPanel.ascx.cs "
            + "ASPxCallbackPanel2_Callback (line 143, Fix Annual Leave Provision), "
            + "ASPxCallbackPanel5_Callback (line 564, Fix Indemnity Provision), "
            + "ASPxCallbackPanel4_Callback (line 922, Fix Total Leave Days) and "
            + "ASPxCallbackPanel6_Callback (line 999, Fix Absent Unpaid Days). "
            + "These retro-recalculate historical SalaryRegisters by priming the legacy PayrollEngine's "
            + "static Program.* context and re-running ~24 hardcoded wage types. This port's engine is "
            + "stateless injected domain services (HCMv3.Core/PayrollManagement/Services/), so this must be "
            + "rebuilt as a 'recalculate historical period' operation on the payroll run orchestrator and "
            + "reconciled against production figures before it can be trusted.");
    }

    #endregion
}
