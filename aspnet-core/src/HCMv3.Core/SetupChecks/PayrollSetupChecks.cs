using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.PayrollManagement;
using HCMv3.Releases;
using HCMv3.SystemSettings;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SetupChecks;

// The payroll set-up the engine refuses to run without. Each was found the hard way by Go-Live Setup: the engine
// stops on it, so a tenant missing it after an upgrade cannot run payroll.

/// <summary>PAY-001: at least one payroll area. Every payroll run is for one area.</summary>
public class PayrollAreaExistsCheck : ISetupCheck, ITransientDependency
{
    private readonly IRepository<PayrollArea> _payrollAreas;

    public PayrollAreaExistsCheck(IRepository<PayrollArea> payrollAreas)
    {
        _payrollAreas = payrollAreas;
    }

    public string Code => "PAY-001";
    public string Module => SetupCheckModules.Payroll;
    public string IntroducedIn => HCMv3Releases.V3_0_0;
    public SetupCheckSeverity Severity => SetupCheckSeverity.Blocker;
    public string FixScreenCode => "SPY025";

    public async Task<SetupCheckFinding> RunAsync(SetupCheckContext context) =>
        SetupCheckFinding.Exists(await _payrollAreas.GetAll().AnyAsync());
}

/// <summary>PAY-002: every payroll area has its control record ("Payroll Control Record not found").</summary>
public class PayrollControlRecordCheck : ISetupCheck, ITransientDependency
{
    private readonly IRepository<PayrollArea> _payrollAreas;
    private readonly IRepository<PayrollControlRecord> _controlRecords;

    public PayrollControlRecordCheck(IRepository<PayrollArea> payrollAreas, IRepository<PayrollControlRecord> controlRecords)
    {
        _payrollAreas = payrollAreas;
        _controlRecords = controlRecords;
    }

    public string Code => "PAY-002";
    public string Module => SetupCheckModules.Payroll;
    public string IntroducedIn => HCMv3Releases.V3_0_0;
    public SetupCheckSeverity Severity => SetupCheckSeverity.Blocker;
    public string FixScreenCode => "SPY027";

    public async Task<SetupCheckFinding> RunAsync(SetupCheckContext context)
    {
        var areas = await _payrollAreas.GetAll().Select(a => new { a.Id, a.Code }).ToListAsync();
        var withRecord = await _controlRecords.GetAll().Select(r => r.PayrollAreaId).Distinct().ToListAsync();
        return SetupCheckFinding.From(areas.Count, areas.Where(a => !withRecord.Contains(a.Id)).Select(a => a.Code).ToList());
    }
}

/// <summary>PAY-003: every payroll area's pay calendar has the month we are in ("Payroll Calender does not have
/// record for Payroll month you are running").</summary>
public class PayrollCalendarCurrentMonthCheck : ISetupCheck, ITransientDependency
{
    private readonly IRepository<PayrollArea> _payrollAreas;
    private readonly IRepository<PayrollCalendar> _calendars;

    public PayrollCalendarCurrentMonthCheck(IRepository<PayrollArea> payrollAreas, IRepository<PayrollCalendar> calendars)
    {
        _payrollAreas = payrollAreas;
        _calendars = calendars;
    }

    public string Code => "PAY-003";
    public string Module => SetupCheckModules.Payroll;
    public string IntroducedIn => HCMv3Releases.V3_0_0;
    public SetupCheckSeverity Severity => SetupCheckSeverity.Blocker;
    public string FixScreenCode => "SPY026";

    public async Task<SetupCheckFinding> RunAsync(SetupCheckContext context)
    {
        var today = context.Today;
        var areas = await _payrollAreas.GetAll().Select(a => new { a.Id, a.Code }).ToListAsync();
        var covered = await _calendars.GetAll()
            .Where(c => c.StartDate <= today && c.EndDate >= today)
            .Select(c => c.PayrollAreaId)
            .Distinct()
            .ToListAsync();
        return SetupCheckFinding.From(areas.Count, areas.Where(a => !covered.Contains(a.Id)).Select(a => a.Code).ToList());
    }
}

/// <summary>PAY-004: the time, payroll, personnel and security settings rows. The time and payroll engines read
/// them without null checks, so a missing row stops a run with no useful message.</summary>
public class SystemSettingsRowsCheck : ISetupCheck, ITransientDependency
{
    private readonly IRepository<ManageTimeSettings> _timeSettings;
    private readonly IRepository<ManagePayrollSettings> _payrollSettings;
    private readonly IRepository<ManagePersonnelSettings> _personnelSettings;
    private readonly IRepository<ManageSecuritySettings> _securitySettings;

    public SystemSettingsRowsCheck(
        IRepository<ManageTimeSettings> timeSettings,
        IRepository<ManagePayrollSettings> payrollSettings,
        IRepository<ManagePersonnelSettings> personnelSettings,
        IRepository<ManageSecuritySettings> securitySettings)
    {
        _timeSettings = timeSettings;
        _payrollSettings = payrollSettings;
        _personnelSettings = personnelSettings;
        _securitySettings = securitySettings;
    }

    public string Code => "PAY-004";
    public string Module => SetupCheckModules.Payroll;
    public string IntroducedIn => HCMv3Releases.V3_0_0;
    public SetupCheckSeverity Severity => SetupCheckSeverity.Blocker;
    public string FixScreenCode => "SST007";

    public async Task<SetupCheckFinding> RunAsync(SetupCheckContext context)
    {
        // Named by their screen codes, which is where support fixes each one.
        var missing = new List<string>();
        if (!await _timeSettings.GetAll().AnyAsync()) missing.Add("SST013");
        if (!await _payrollSettings.GetAll().AnyAsync()) missing.Add("SST007");
        if (!await _personnelSettings.GetAll().AnyAsync()) missing.Add("SST008");
        if (!await _securitySettings.GetAll().AnyAsync()) missing.Add("SST011");
        return SetupCheckFinding.From(4, missing);
    }
}

/// <summary>PAY-005: leave-pay links ("which pay counts during leave"). With none, the annual leave provision
/// calculates to zero while payroll otherwise runs normally.</summary>
public class WageTypeLeaveLinksCheck : ISetupCheck, ITransientDependency
{
    private readonly IRepository<WageTypeLeave> _links;

    public WageTypeLeaveLinksCheck(IRepository<WageTypeLeave> links)
    {
        _links = links;
    }

    public string Code => "PAY-005";
    public string Module => SetupCheckModules.Payroll;
    public string IntroducedIn => HCMv3Releases.V3_0_0;
    public SetupCheckSeverity Severity => SetupCheckSeverity.Warning;
    public string FixScreenCode => "SPY056";

    public async Task<SetupCheckFinding> RunAsync(SetupCheckContext context) =>
        SetupCheckFinding.Exists(await _links.GetAll().AnyAsync());
}
