using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee asset assignment record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeAsset</c>, Wave 4 deferred bucket).
///
/// <see cref="AssetId"/> FKs <c>HCMv3.GeneralMasterInfo.Asset</c> (already ported). The legacy
/// <c>DocAttachment</c> FK is dropped, consistent with every other entity in this port.
/// </summary>
public class EmployeeAsset : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int AssetId { get; set; }

    public virtual Asset Asset { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual AssetDeductionMode AssetDeductionMode { get; set; }

    public virtual string Comments { get; set; }
}
