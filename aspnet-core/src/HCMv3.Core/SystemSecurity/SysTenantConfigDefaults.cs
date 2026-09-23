using System;
using System.Collections.Generic;

namespace HCMv3.SystemSecurity;

/// <summary>
/// The <see cref="SysTenantConfig"/> rows every tenant starts with. Ported from legacy
/// <c>DefaultDataService.CreateDefaultSysTenantConfig</c>: same nine keys, same values, same open-ended
/// date range. New tenants get them from <see cref="SysTenantConfigDefaultsSeeder"/>; existing tenants get
/// them from the startup seed (<c>SysTenantConfigDefaultsBuilder</c>).
///
/// These rows matter because <c>SysSettingsAccessor</c> builds a tenant's first <c>Manage*Settings</c> row
/// from them (legacy <c>SysService.Get*Settings</c>): without them a fresh tenant would start with 0
/// working hours and no earliest time-evaluation date, where legacy started with 9 hours and 01/01/2017.
///
/// Legacy left <c>ConfigCategory</c> at its default on every seeded row; this port files each row under
/// the category the screen groups it by, which changes nothing about how the value is read.
/// </summary>
public static class SysTenantConfigDefaults
{
    public static readonly DateTime LegacyStartDate = new(1800, 1, 1);
    public static readonly DateTime LegacyEndDate = new(9999, 12, 31);

    /// <summary>The dd/MM/yyyy layout legacy stores <c>TIME_EVAL_EARLIEST_DATE</c> in.</summary>
    public const string LegacyDateFormat = "dd/MM/yyyy";

    public sealed record Entry(string Key, string KeyValue, TenantConfigCategory Category, string Description);

    public static readonly IReadOnlyList<Entry> Entries = new[]
    {
        new Entry(SysTenantConfigKeys.DefaultWorkingHours, "9", TenantConfigCategory.Payroll,
            "Default working hours per day used when no duty period applies."),
        new Entry(SysTenantConfigKeys.TimeEvalEarliestDate, "01/01/2017", TenantConfigCategory.TimeAttendance,
            "Earliest date the time engine evaluates (dd/MM/yyyy)."),
        new Entry(SysTenantConfigKeys.CutoverDate, "01/01/2017", TenantConfigCategory.TimeAttendance,
            "Legacy cut-over date; kept for data parity, not read by the application."),
        new Entry(SysTenantConfigKeys.DateEditMask, "dd/MM/yyyy", TenantConfigCategory.Formatting,
            "Date edit mask."),
        new Entry(SysTenantConfigKeys.DateDisplayFormat, "{0:dd/MM/yyyy}", TenantConfigCategory.Formatting,
            "Date display format."),
        new Entry(SysTenantConfigKeys.TimeEditMask, "HH:mm", TenantConfigCategory.Formatting,
            "Time edit mask."),
        new Entry(SysTenantConfigKeys.TimeDisplayFormat, "{0:HH\\:mm}", TenantConfigCategory.Formatting,
            "Time display format."),
        new Entry(SysTenantConfigKeys.EnableTimeAttendanceIntegration, "1", TenantConfigCategory.TimeAttendance,
            "1 = time attendance integration is on."),
        new Entry(SysTenantConfigKeys.LeaveConflictCheck, "Department", TenantConfigCategory.Employee,
            "Legacy leave conflict scope; kept for data parity, not read by the application.")
    };
}
