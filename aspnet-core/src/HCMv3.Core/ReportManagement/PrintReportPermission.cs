using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.Authorization.Roles;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.ReportManagement;

/// <summary>
/// Grants a Role access to a set of <see cref="PrintReport"/>s (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ReportManagement.PrintReportPermission</c>). FKs ASP.NET Zero's own
/// built-in <see cref="Role"/> entity rather than the legacy's <c>PermissionPolicyRole</c> -- same pattern
/// as <c>HCMv3.PayrollManagement.WageTypeSecurityRole</c>. The aggregated <c>PrintReportPermissionDetail</c>
/// child collection is ported as a flat satellite entity (<see cref="PrintReportPermissionDetail"/>) carrying
/// its own <c>PrintReportPermissionId</c> FK, same flat parent+child pattern used throughout this port. The
/// legacy's <c>PrintReportPermissionCopy</c> transient wizard object ([NonPersistent]) is NOT ported.
/// </summary>
public class PrintReportPermission : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    public virtual int RoleId { get; set; }

    public virtual Role Role { get; set; }
}
