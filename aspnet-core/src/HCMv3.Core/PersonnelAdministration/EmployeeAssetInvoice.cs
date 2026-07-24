using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee asset invoice record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeAssetInvoice</c>, Wave 4 deferred
/// bucket).
///
/// <see cref="AssetId"/> FKs <c>HCMv3.GeneralMasterInfo.Asset</c> (already ported). The legacy
/// <c>DocAttachment</c> FK is dropped.
///
/// DELIBERATE SIMPLIFICATION: the legacy <c>EmployeePlanAmount</c>/<c>DeductionAmount</c> computed
/// properties and the <c>AddMobileDeduction</c>/<c>DeleteMobileDeduction</c> cascade (which creates/updates
/// an offsetting <c>EmployeePayment</c> row) are genuinely cross-entity business logic dependent on
/// <c>EmployeeSubGroup.AssetAllowance</c> (ManpowerPlanning/Payroll salary-profile module, not yet ported)
/// -- NOT ported on the entity itself; only <see cref="DeductionAmount"/> is kept as a stored scalar field
/// (no longer computed) so the invoice amount/deduction relationship can still be recorded manually. This
/// mirrors the "not ported on the entity itself, consistent with every other cross-entity cascade" pattern
/// used throughout Waves 1-3 (e.g. <c>EmployeeSponsor</c>'s quota cascade, which IS ported at the app-service
/// layer -- here there is no equivalent Payroll salary-profile entity to cascade against, so it is dropped
/// entirely).
/// </summary>
public class EmployeeAssetInvoice : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int AssetId { get; set; }

    public virtual Asset Asset { get; set; }

    public virtual DateTime InvoiceDate { get; set; }

    public virtual double InvoiceAmount { get; set; }

    /// <summary>Legacy computed <c>EmployeePlanAmount</c>, ported as a plain stored field (see class remarks).</summary>
    public virtual double EmployeePlanAmount { get; set; }

    /// <summary>Legacy computed <c>DeductionAmount</c>, ported as a plain stored field (see class remarks).</summary>
    public virtual double DeductionAmount { get; set; }

    public virtual bool DeductFromEmployee { get; set; }

    public virtual string Comments { get; set; }
}
