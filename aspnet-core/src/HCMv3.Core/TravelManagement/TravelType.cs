using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;

namespace HCMv3.TravelManagement;

/// <summary>
/// TravelType master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TravelManagement.TravelType</c>). Extends the Code/NameEn/NameAr shape
/// with a <see cref="WageTypeId"/> FK (PayrollManagement), so it can't derive from
/// <see cref="SimpleLookupEntity"/> (same bespoke pattern as <c>GeneralMasterInfo.LoanType</c>).
///
/// DELIBERATE SIMPLIFICATION: the legacy non-persistent <c>TravelTypeWageType</c> collection (a UI-only
/// computed dropdown-filter helper that excluded WageTypes already consumed by
/// OvertimeType/AbsenceType/TicketType/LoanType) is presentation logic, not a stored field, and is NOT
/// ported. It can be reimplemented client-side later if needed. The legacy <c>IExcelImport</c> marker is an
/// XAF UI concept and is not ported.
/// </summary>
public class TravelType : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameAr { get; set; }

    public virtual int? WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }
}
