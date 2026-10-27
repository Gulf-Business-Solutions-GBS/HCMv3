namespace HCMv3.SystemSecurity;

/// <summary>
/// Functional area a <c>SysTenantConfig</c> date-effective key/value row applies to (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSecurity.ConfigCategory</c>). Renamed from the legacy bare
/// <c>ConfigCategory</c> to avoid colliding with the entity's own <c>ConfigCategory</c> property name.
/// </summary>
public enum TenantConfigCategory
{
    Employee = 0,
    Payroll = 1,
    TimeAttendance = 2,
    Formatting = 3,
    Report = 4,
    Geolocation = 5
}
