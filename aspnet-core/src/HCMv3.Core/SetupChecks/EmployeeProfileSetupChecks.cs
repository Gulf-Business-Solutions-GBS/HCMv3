using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Releases;
using HCMv3.TimeManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SetupChecks;

/// <summary>
/// A check over every ACTIVE employee who has joined by today: which of them lack one record the payroll or time
/// engine needs. Examples are employee codes only.
/// </summary>
public abstract class ActiveEmployeeSetupCheck : ISetupCheck, ITransientDependency
{
    private readonly IRepository<Employee> _employees;

    protected ActiveEmployeeSetupCheck(IRepository<Employee> employees)
    {
        _employees = employees;
    }

    public abstract string Code { get; }
    public string Module => SetupCheckModules.EmployeeProfile;
    public abstract string IntroducedIn { get; }
    public abstract SetupCheckSeverity Severity { get; }

    /// <summary>Most employee records are kept on the Employees screen.</summary>
    public virtual string FixScreenCode => "SPA007";

    public async Task<SetupCheckFinding> RunAsync(SetupCheckContext context)
    {
        var active = await ActiveEmployees(context.Today).Select(e => new { e.Id, e.Code }).ToListAsync();
        if (active.Count == 0)
        {
            return SetupCheckFinding.NotApplicable();
        }

        var having = (await EmployeesHavingItAsync(context.Today)).ToHashSet();
        return SetupCheckFinding.From(active.Count, active.Where(e => !having.Contains(e.Id)).Select(e => e.Code).ToList());
    }

    /// <summary>Ids of the employees who have what this check looks for, as of <paramref name="today"/>.</summary>
    protected abstract Task<List<int>> EmployeesHavingItAsync(DateTime today);

    protected IQueryable<Employee> ActiveEmployees(DateTime today) =>
        _employees.GetAll().Where(e => e.Status == EmployeeStatus.Active && e.JoiningDate <= today);
}

/// <summary>EMP-001: a contract covering today ("Employee Contract not found for Employee X").</summary>
public class EmployeeContractCheck : ActiveEmployeeSetupCheck
{
    private readonly IRepository<EmployeeContract> _contracts;

    public EmployeeContractCheck(IRepository<Employee> employees, IRepository<EmployeeContract> contracts) : base(employees)
    {
        _contracts = contracts;
    }

    public override string Code => "EMP-001";
    public override string IntroducedIn => HCMv3Releases.V3_0_0;
    public override SetupCheckSeverity Severity => SetupCheckSeverity.Blocker;

    protected override Task<List<int>> EmployeesHavingItAsync(DateTime today) =>
        _contracts.GetAll().Where(c => c.StartDate <= today && c.EndDate >= today).Select(c => c.EmployeeId).Distinct().ToListAsync();
}

/// <summary>EMP-002: an org assignment covering today.</summary>
public class EmployeeOrgAssignmentCheck : ActiveEmployeeSetupCheck
{
    private readonly IRepository<EmployeeOrgAssignment> _assignments;

    public EmployeeOrgAssignmentCheck(IRepository<Employee> employees, IRepository<EmployeeOrgAssignment> assignments) : base(employees)
    {
        _assignments = assignments;
    }

    public override string Code => "EMP-002";
    public override string IntroducedIn => HCMv3Releases.V3_0_0;
    public override SetupCheckSeverity Severity => SetupCheckSeverity.Blocker;

    protected override Task<List<int>> EmployeesHavingItAsync(DateTime today) =>
        _assignments.GetAll().Where(a => a.StartDate <= today && a.EndDate >= today).Select(a => a.EmployeeId).Distinct().ToListAsync();
}

/// <summary>
/// EMP-003: a payroll area through today's org assignment. Read the way the engine reads it
/// (<see cref="PayrollManagement.Services.IEmployeePayrollAreaResolver"/>): the assignment's own payroll area, or else
/// the payroll area on its organisation unit's profile. Without one the employee is never picked up by a run.
/// </summary>
public class EmployeePayrollAreaCheck : ActiveEmployeeSetupCheck
{
    private readonly IRepository<EmployeeOrgAssignment> _assignments;
    private readonly IRepository<OrgUnitProfile> _orgUnitProfiles;

    public EmployeePayrollAreaCheck(
        IRepository<Employee> employees,
        IRepository<EmployeeOrgAssignment> assignments,
        IRepository<OrgUnitProfile> orgUnitProfiles) : base(employees)
    {
        _assignments = assignments;
        _orgUnitProfiles = orgUnitProfiles;
    }

    public override string Code => "EMP-003";
    public override string IntroducedIn => HCMv3Releases.V3_0_0;
    public override SetupCheckSeverity Severity => SetupCheckSeverity.Blocker;

    protected override async Task<List<int>> EmployeesHavingItAsync(DateTime today)
    {
        var unitsWithArea = await _orgUnitProfiles.GetAll()
            .Where(p => p.PayrollAreaId != null)
            .Select(p => p.OrganizationUnitId)
            .ToListAsync();

        return await _assignments.GetAll()
            .Where(a => a.StartDate <= today && a.EndDate >= today)
            .Where(a => a.PayrollAreaId != null
                        || (a.OrganizationUnitId != null && unitsWithArea.Contains(a.OrganizationUnitId.Value)))
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToListAsync();
    }
}

/// <summary>EMP-004: an employee group and sub group on today's org assignment. They carry the indemnity, gratuity
/// and leave rules, so without them those amounts are not calculated.</summary>
public class EmployeeGroupAndSubGroupCheck : ActiveEmployeeSetupCheck
{
    private readonly IRepository<EmployeeOrgAssignment> _assignments;

    public EmployeeGroupAndSubGroupCheck(IRepository<Employee> employees, IRepository<EmployeeOrgAssignment> assignments) : base(employees)
    {
        _assignments = assignments;
    }

    public override string Code => "EMP-004";
    public override string IntroducedIn => HCMv3Releases.V3_0_0;
    public override SetupCheckSeverity Severity => SetupCheckSeverity.Blocker;

    protected override Task<List<int>> EmployeesHavingItAsync(DateTime today) =>
        _assignments.GetAll()
            .Where(a => a.StartDate <= today && a.EndDate >= today && a.EmployeeGroupId != null && a.EmployeeSubGroupId != null)
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToListAsync();
}

/// <summary>EMP-005: a salary record covering today. Without one the employee is paid nothing.</summary>
public class EmployeeSalaryRecordCheck : ActiveEmployeeSetupCheck
{
    private readonly IRepository<EmployeeBasicPay> _basicPays;

    public EmployeeSalaryRecordCheck(IRepository<Employee> employees, IRepository<EmployeeBasicPay> basicPays) : base(employees)
    {
        _basicPays = basicPays;
    }

    public override string Code => "EMP-005";
    public override string IntroducedIn => HCMv3Releases.V3_0_0;
    public override SetupCheckSeverity Severity => SetupCheckSeverity.Blocker;

    protected override Task<List<int>> EmployeesHavingItAsync(DateTime today) =>
        _basicPays.GetAll().Where(p => p.StartDate <= today && p.EndDate >= today).Select(p => p.EmployeeId).Distinct().ToListAsync();
}

/// <summary>EMP-006: a payroll status row ("Payroll Status not found for Employee"). The time engine creates it the
/// first time it sees the employee, so the fix is a time engine run.</summary>
public class EmployeePayrollStatusCheck : ActiveEmployeeSetupCheck
{
    private readonly IRepository<EmployeePayrollStatus> _statuses;

    public EmployeePayrollStatusCheck(IRepository<Employee> employees, IRepository<EmployeePayrollStatus> statuses) : base(employees)
    {
        _statuses = statuses;
    }

    public override string Code => "EMP-006";
    public override string IntroducedIn => HCMv3Releases.V3_0_0;
    public override SetupCheckSeverity Severity => SetupCheckSeverity.Blocker;
    public override string FixScreenCode => "STM022";

    protected override Task<List<int>> EmployeesHavingItAsync(DateTime today) =>
        _statuses.GetAll().Select(s => s.EmployeeId).Distinct().ToListAsync();
}

/// <summary>EMP-007: a duty period covering today ("Employee Duty Period not found"). The time engine needs it to
/// know the working days.</summary>
public class EmployeeDutyPeriodCheck : ActiveEmployeeSetupCheck
{
    private readonly IRepository<EmployeeDutyPeriod> _dutyPeriods;

    public EmployeeDutyPeriodCheck(IRepository<Employee> employees, IRepository<EmployeeDutyPeriod> dutyPeriods) : base(employees)
    {
        _dutyPeriods = dutyPeriods;
    }

    public override string Code => "EMP-007";
    public override string IntroducedIn => HCMv3Releases.V3_0_0;
    public override SetupCheckSeverity Severity => SetupCheckSeverity.Blocker;

    protected override Task<List<int>> EmployeesHavingItAsync(DateTime today) =>
        _dutyPeriods.GetAll().Where(d => d.StartDate <= today && d.EndDate >= today).Select(d => d.EmployeeId).Distinct().ToListAsync();
}

/// <summary>EMP-008: a time policy covering today. The time engine needs it to know whether to expect punches.</summary>
public class EmployeeTimePolicyCheck : ActiveEmployeeSetupCheck
{
    private readonly IRepository<EmployeeTimePolicy> _timePolicies;

    public EmployeeTimePolicyCheck(IRepository<Employee> employees, IRepository<EmployeeTimePolicy> timePolicies) : base(employees)
    {
        _timePolicies = timePolicies;
    }

    public override string Code => "EMP-008";
    public override string IntroducedIn => HCMv3Releases.V3_0_0;
    public override SetupCheckSeverity Severity => SetupCheckSeverity.Blocker;

    protected override Task<List<int>> EmployeesHavingItAsync(DateTime today) =>
        _timePolicies.GetAll().Where(t => t.StartDate <= today && t.EndDate >= today).Select(t => t.EmployeeId).Distinct().ToListAsync();
}

/// <summary>EMP-009: payment details (bank, cash...) covering today. Payroll still runs, but the bank file leaves
/// the employee out.</summary>
public class EmployeePaymentDetailsCheck : ActiveEmployeeSetupCheck
{
    private readonly IRepository<EmployeeBank> _banks;

    public EmployeePaymentDetailsCheck(IRepository<Employee> employees, IRepository<EmployeeBank> banks) : base(employees)
    {
        _banks = banks;
    }

    public override string Code => "EMP-009";
    public override string IntroducedIn => HCMv3Releases.V3_0_0;
    public override SetupCheckSeverity Severity => SetupCheckSeverity.Warning;

    protected override Task<List<int>> EmployeesHavingItAsync(DateTime today) =>
        _banks.GetAll().Where(b => b.StartDate <= today && b.EndDate >= today).Select(b => b.EmployeeId).Distinct().ToListAsync();
}

/// <summary>
/// EMP-010: employees paid by bank have an IBAN or an account number. Only they are checked: employees paid in cash,
/// or with no payment details at all (that is EMP-009), are not counted.
/// </summary>
public class EmployeeBankAccountCheck : ISetupCheck, ITransientDependency
{
    private readonly IRepository<Employee> _employees;
    private readonly IRepository<EmployeeBank> _banks;

    public EmployeeBankAccountCheck(IRepository<Employee> employees, IRepository<EmployeeBank> banks)
    {
        _employees = employees;
        _banks = banks;
    }

    public string Code => "EMP-010";
    public string Module => SetupCheckModules.EmployeeProfile;
    public string IntroducedIn => HCMv3Releases.V3_0_0;
    public SetupCheckSeverity Severity => SetupCheckSeverity.Warning;
    public string FixScreenCode => "SPA007";

    public async Task<SetupCheckFinding> RunAsync(SetupCheckContext context)
    {
        var today = context.Today;
        var bankPaid = await (
                from bank in _banks.GetAll()
                join employee in _employees.GetAll() on bank.EmployeeId equals employee.Id
                where employee.Status == EmployeeStatus.Active && employee.JoiningDate <= today
                      && bank.StartDate <= today && bank.EndDate >= today
                      && bank.PaymentType == PaymentType.Bank
                select new { employee.Code, bank.IBAN, bank.AccountNum })
            .ToListAsync();

        // An employee may have two current rows; one with an account is enough.
        var byEmployee = bankPaid.GroupBy(b => b.Code).ToList();
        var missing = byEmployee
            .Where(g => g.All(b => string.IsNullOrWhiteSpace(b.IBAN) && string.IsNullOrWhiteSpace(b.AccountNum)))
            .Select(g => g.Key)
            .ToList();
        return SetupCheckFinding.From(byEmployee.Count, missing);
    }
}
