using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Registry of persistent object/screen classes, describing payroll/time/permission sensitivity flags used
/// elsewhere by the engines (ported from legacy XAF HCM10.Module.BusinessObjects.SysSecurity.SysObject).
/// Legacy IsPayrollSensitive was an int flag; ported here as a bool.
/// </summary>
public class SysObject : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string Name { get; set; }

    [StringLength(500)]
    public virtual string FullName { get; set; }

    public virtual bool IsPayrollSensitive { get; set; }

    public virtual bool RespectPermission { get; set; }

    /// <summary>
    /// When IsPayrollSensitive is true, allows retro run even if the global
    /// DisableRetroCalculation setting is on.
    /// </summary>
    public virtual bool MustRetroRun { get; set; }

    public virtual bool IsTimeSensitive { get; set; }
}
