using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// EmployeeSubGroup master, child of <see cref="EmployeeGroup"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.EmployeeSubGroup</c>).
///
/// DELIBERATE SIMPLIFICATION: the legacy's 13 <c>[Aggregated]</c> <c>XPCollection</c> navigation properties
/// (Leave, Gratuity, Ticket, Payment, Fund, AssetAllowance, HealthReimburse, Bonus, LeaveType, WageType,
/// Commission, Benefit) are NOT modeled here as bidirectional collections. Following the flat parent+child
/// pattern already used elsewhere in this codebase (e.g. TimeManagement's HolidayCalendar/Holiday), each
/// child entity instead just carries its own <c>EmployeeSubGroupId</c> FK. <c>TicketClassId</c> is a nullable
/// FK to the not-yet-ported <c>TicketClass</c> lookup (a later wave); the legacy <c>IExcelImport</c> marker
/// is an XAF UI concept and is not ported.
/// </summary>
public class EmployeeSubGroup : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeGroupId { get; set; }

    public virtual EmployeeGroup EmployeeGroup { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameAr { get; set; }

    /// <summary>
    /// FK to TicketClass (not yet ported in this codebase -- a later wave). Nullable so this column can be
    /// added now and resolved once that wave lands.
    /// </summary>
    public virtual int? TicketClassId { get; set; }

    public virtual bool PayOvertime { get; set; }

    public virtual AirTicketPayType AirTicketPayType { get; set; }

    public virtual double MaxHealthReimburseAmount { get; set; }

    /// <summary>
    /// Reuses <see cref="CustomStartMonth"/> (None + 12 months) -- the legacy
    /// <c>HealthInsurancePeriod</c> enum has the identical value set, so no separate enum is declared.
    /// </summary>
    public virtual CustomStartMonth HealthInsurancePeriod { get; set; }

    public virtual bool AutoPayAirTicket { get; set; }

    public virtual int MaxDependantAirTicketCount { get; set; }

    public virtual int MaxDependantHealthReimburseCount { get; set; }
}
