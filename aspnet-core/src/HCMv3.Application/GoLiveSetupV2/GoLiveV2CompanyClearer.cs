using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Organizations;
using Abp.UI;
using HCMv3.BenefitsManagement;
using HCMv3.EngineRuns;
using HCMv3.GoLiveSetupV2.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GoLiveSetupV2;

/// <summary>
/// Takes a go-live payroll area back to no employees and no payroll month, so the sheet can be loaded again
/// and a month -- an earlier one included -- set up again.
///
/// <para><b>What goes.</b> The area's employees, and everything that belongs to them: what the load made
/// (contract, org assignment, salary, payroll status, working time, bank, sponsor), what the time engine
/// wrote (time registers and their logs, leave and indemnity quotas, overtime, attendance, tickets), and what
/// the payroll engine wrote (salary registers with their lines and days, payments, loans, funds, insurance,
/// logs, run progress). Then the area's control record and pay calendar. The list is every table the two
/// engines write per employee; because the employees themselves go, their balances go with them and nothing
/// needs recalculating.</para>
///
/// <para><b>What stays.</b> The company's setup: the org unit, the payroll area, the grade and its rules,
/// the working time, the banks and their codes. The next load reuses all of it.</para>
///
/// <para>Every delete is ABP's soft delete, so the rows are kept for audit and an employee code is free
/// again: the employee service checks codes only among employees not deleted.</para>
/// </summary>
public class GoLiveV2CompanyClearer : AbpServiceBase, ITransientDependency
{
    private readonly IIocResolver _iocResolver;
    private readonly IRepository<PayrollArea> _payrollAreas;
    private readonly IRepository<EmployeeOrgAssignment> _assignments;
    private readonly IRepository<OrganizationUnit, long> _orgUnits;

    public GoLiveV2CompanyClearer(
        IIocResolver iocResolver,
        IRepository<PayrollArea> payrollAreas,
        IRepository<EmployeeOrgAssignment> assignments,
        IRepository<OrganizationUnit, long> orgUnits)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;

        _iocResolver = iocResolver;
        _payrollAreas = payrollAreas;
        _assignments = assignments;
        _orgUnits = orgUnits;
    }

    public async Task<GoLiveV2ClearCompanyDto> PreviewAsync(string payrollAreaName)
    {
        var scope = await ScopeAsync(payrollAreaName);
        return scope == null ? new GoLiveV2ClearCompanyDto() : await RunAsync(scope, delete: false);
    }

    public async Task<GoLiveV2ClearCompanyDto> ClearAsync(GoLiveV2ClearCompanyInput input)
    {
        var scope = await ScopeAsync(input.PayrollAreaName);
        if (scope == null || scope.EmployeeIds.Count == 0 && scope.CompanyName == null)
        {
            return await PreviewAsync(input.PayrollAreaName);
        }

        if (!string.Equals(input.ConfirmCompanyName?.Trim(), scope.CompanyName?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new UserFriendlyException(L("GoLiveV2ClearNameMismatch", scope.CompanyName));
        }

        var removed = await RunAsync(scope, delete: true);
        await CurrentUnitOfWork.SaveChangesAsync();
        return removed;
    }

    /// <summary>
    /// Counts, and when asked deletes, in dependency order: a child before the row it hangs off, the
    /// employees after everything that belongs to them, the payroll month last.
    /// </summary>
    private async Task<GoLiveV2ClearCompanyDto> RunAsync(Scope scope, bool delete)
    {
        var ids = scope.EmployeeIds;
        var result = new GoLiveV2ClearCompanyDto { CompanyName = scope.CompanyName };

        int Other(int count)
        {
            result.OtherRecords += count;
            return count;
        }

        // What the payroll engine wrote.
        var registerIds = await IdsAsync<SalaryRegister>(r => ids.Contains(r.EmployeeId));
        var detailIds = await IdsAsync<SalaryRegisterDetail>(d => registerIds.Contains(d.SalaryRegisterId));
        Other(await RemoveAsync<SalaryRegisterDaysDetail>(d => detailIds.Contains(d.SalaryRegisterDetailId), delete));
        Other(await RemoveAsync<SalaryRegisterDetail>(d => registerIds.Contains(d.SalaryRegisterId), delete));
        result.SalaryRegisters = await RemoveAsync<SalaryRegister>(r => ids.Contains(r.EmployeeId), delete);

        var loanIds = await IdsAsync<EmployeeLoan>(l => ids.Contains(l.EmployeeId));
        Other(await RemoveAsync<EmployeeLoanRecovery>(r => loanIds.Contains(r.EmployeeLoanId), delete));
        Other(await RemoveAsync<EmployeeLoan>(l => ids.Contains(l.EmployeeId), delete));

        var fundIds = await IdsAsync<EmployeeFund>(f => ids.Contains(f.EmployeeId));
        Other(await RemoveAsync<EmployeeFundDetail>(d => fundIds.Contains(d.EmployeeFundId), delete));
        Other(await RemoveAsync<EmployeeFund>(f => ids.Contains(f.EmployeeId), delete));

        var insuranceIds = await IdsAsync<EmployeeInsurance>(i => ids.Contains(i.EmployeeId));
        Other(await RemoveAsync<EmployeeInsuranceDetail>(d => insuranceIds.Contains(d.EmployeeInsuranceId), delete));
        Other(await RemoveAsync<CompanyInsuranceDetail>(d => insuranceIds.Contains(d.EmployeeInsuranceId), delete));
        Other(await RemoveAsync<EmployeeInsurance>(i => ids.Contains(i.EmployeeId), delete));

        Other(await RemoveAsync<EmployeePayment>(p => ids.Contains(p.EmployeeId), delete));
        Other(await RemoveAsync<EmployeeAbsence>(a => ids.Contains(a.EmployeeId), delete));
        Other(await RemoveAsync<PayrollLog>(l => l.EmployeeId != null && ids.Contains(l.EmployeeId.Value), delete));

        // What the time engine wrote.
        var timeRegisterIds = await IdsAsync<TimeRegister>(r => ids.Contains(r.EmployeeId));
        Other(await RemoveAsync<TimeLogHistory>(h => timeRegisterIds.Contains(h.TimeRegisterId), delete));
        result.TimeRegisters = await RemoveAsync<TimeRegister>(r => ids.Contains(r.EmployeeId), delete);
        Other(await RemoveAsync<TimeLog>(l => l.EmployeeId != null && ids.Contains(l.EmployeeId.Value), delete));
        Other(await RemoveAsync<EmployeeTicket>(t => ids.Contains(t.EmployeeId), delete));
        Other(await RemoveAsync<EmployeeAbsenceQuota>(q => ids.Contains(q.EmployeeId), delete));
        Other(await RemoveAsync<EmployeeGratuityQuota>(q => ids.Contains(q.EmployeeId), delete));
        Other(await RemoveAsync<EmployeeAbsQuotaForfeit>(f => ids.Contains(f.EmployeeId), delete));
        Other(await RemoveAsync<EmployeeAttendance>(a => ids.Contains(a.EmployeeId), delete));
        Other(await RemoveAsync<EmployeeOvertime>(o => ids.Contains(o.EmployeeId), delete));
        Other(await RemoveAsync<EngineRunEmployee, long>(e => e.EmployeeId != null && ids.Contains(e.EmployeeId.Value), delete));

        // What the load made.
        var payIds = await IdsAsync<EmployeeBasicPay>(p => ids.Contains(p.EmployeeId));
        Other(await RemoveAsync<EmployeeBasicPayDetail>(d => payIds.Contains(d.EmployeeBasicPayId), delete));
        Other(await RemoveAsync<EmployeeBasicPay>(p => ids.Contains(p.EmployeeId), delete));
        Other(await RemoveAsync<EmployeeContract>(c => ids.Contains(c.EmployeeId), delete));
        Other(await RemoveAsync<EmployeePayrollStatus>(s => ids.Contains(s.EmployeeId), delete));
        Other(await RemoveAsync<EmployeeDutyPeriod>(d => ids.Contains(d.EmployeeId), delete));
        Other(await RemoveAsync<EmployeeTimePolicy>(t => ids.Contains(t.EmployeeId), delete));
        Other(await RemoveAsync<EmployeeBank>(b => ids.Contains(b.EmployeeId), delete));
        Other(await RemoveAsync<TimeTransfer>(t => ids.Contains(t.EmployeeId), delete));
        Other(await RemoveAsync<EmployeeSponsor>(s => ids.Contains(s.EmployeeId), delete));
        Other(await RemoveAsync<EmployeeOrgAssignment>(a => ids.Contains(a.EmployeeId), delete));
        result.Employees = await RemoveAsync<Employee>(e => ids.Contains(e.Id), delete);

        // The payroll month, so any month can be set up next -- an earlier one included.
        var areaId = scope.PayrollAreaId;
        result.PayrollMonthEnd = await FirstControlRecordMonthEndAsync(areaId);
        await RemoveAsync<PayrollControlRecord>(r => r.PayrollAreaId == areaId, delete);
        result.CalendarMonths = await RemoveAsync<PayrollCalendar>(c => c.PayrollAreaId == areaId, delete);

        return result;
    }

    private async Task<Scope> ScopeAsync(string payrollAreaName)
    {
        var name = payrollAreaName?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        var area = await _payrollAreas.FirstOrDefaultAsync(a => a.Name == name);
        if (area == null)
        {
            return null;
        }

        // Everyone the load put in this area, whatever the dates: all of them are the area's.
        var assignments = await _assignments.GetAll()
            .Where(a => a.PayrollAreaId == area.Id)
            .Select(a => new { a.EmployeeId, a.OrganizationUnitId })
            .ToListAsync();

        var unitId = assignments
            .Where(a => a.OrganizationUnitId != null)
            .GroupBy(a => a.OrganizationUnitId.Value)
            .OrderByDescending(g => g.Count())
            .Select(g => (long?)g.Key)
            .FirstOrDefault();

        return new Scope
        {
            PayrollAreaId = area.Id,
            EmployeeIds = assignments.Select(a => a.EmployeeId).Distinct().ToList(),
            CompanyName = unitId == null ? null : (await _orgUnits.FirstOrDefaultAsync(unitId.Value))?.DisplayName,
        };
    }

    private async Task<DateTime?> FirstControlRecordMonthEndAsync(int payrollAreaId)
    {
        using var records = _iocResolver.ResolveAsDisposable<IRepository<PayrollControlRecord>>();
        return await records.Object.GetAll()
            .Where(r => r.PayrollAreaId == payrollAreaId)
            .Select(r => (DateTime?)r.CalendarMonthEnd)
            .FirstOrDefaultAsync();
    }

    private async Task<List<int>> IdsAsync<TEntity>(Expression<Func<TEntity, bool>> where)
        where TEntity : class, IEntity<int>
    {
        using var repository = _iocResolver.ResolveAsDisposable<IRepository<TEntity>>();
        return await repository.Object.GetAll().Where(where).Select(e => e.Id).ToListAsync();
    }

    private Task<int> RemoveAsync<TEntity>(Expression<Func<TEntity, bool>> where, bool delete)
        where TEntity : class, IEntity<int> =>
        RemoveAsync<TEntity, int>(where, delete);

    /// <summary>Counts the rows; deletes them too when asked. Soft delete: ABP marks them, nothing is lost.</summary>
    private async Task<int> RemoveAsync<TEntity, TKey>(Expression<Func<TEntity, bool>> where, bool delete)
        where TEntity : class, IEntity<TKey>
    {
        using var repository = _iocResolver.ResolveAsDisposable<IRepository<TEntity, TKey>>();
        var rows = await repository.Object.GetAll().Where(where).ToListAsync();

        if (delete)
        {
            foreach (var row in rows)
            {
                await repository.Object.DeleteAsync(row);
            }
        }

        return rows.Count;
    }

    private sealed class Scope
    {
        public int PayrollAreaId { get; init; }

        public List<int> EmployeeIds { get; init; }

        public string CompanyName { get; init; }
    }
}
