using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Low-value free-text benefit-grade reference grid (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.TempBenefitType</c>). <see cref="Grade"/> plus ~24
/// free-text string columns, none of which are typed/FK'd into the real payroll structures in the legacy
/// system either -- ported faithfully as a simple bespoke entity, not restructured. <see cref="Grade"/> must
/// be unique (enforced in <c>TempBenefitTypesAppService</c>, legacy "Names already In Use" parity).
/// </summary>
public class TempBenefitType : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string Grade { get; set; }

    [StringLength(256)]
    public virtual string Ticket { get; set; }

    [StringLength(256)]
    public virtual string SimCard { get; set; }

    [StringLength(256)]
    public virtual string Phone { get; set; }

    [StringLength(256)]
    public virtual string Incentive { get; set; }

    [StringLength(256)]
    public virtual string BonusSIP { get; set; }

    [StringLength(256)]
    public virtual string Uniform { get; set; }

    [StringLength(256)]
    public virtual string MedicalInsurance { get; set; }

    [StringLength(256)]
    public virtual string LeaveDays { get; set; }

    [StringLength(256)]
    public virtual string MedicalLeave { get; set; }

    [StringLength(256)]
    public virtual string MaternityLeave { get; set; }

    [StringLength(256)]
    public virtual string StaffDiscount { get; set; }

    [StringLength(256)]
    public virtual string SpecialAllowance { get; set; }

    [StringLength(256)]
    public virtual string ManagerAllowance { get; set; }

    [StringLength(256)]
    public virtual string CompanyAsset { get; set; }

    [StringLength(256)]
    public virtual string SchoolAllowance { get; set; }

    [StringLength(256)]
    public virtual string NoticePeriod { get; set; }

    [StringLength(256)]
    public virtual string Indemnity { get; set; }

    [StringLength(256)]
    public virtual string Retirement { get; set; }

    [StringLength(256)]
    public virtual string MinSalary { get; set; }

    [StringLength(256)]
    public virtual string MidSalary { get; set; }

    [StringLength(256)]
    public virtual string MaxSalary { get; set; }

    [StringLength(256)]
    public virtual string TransportationAllowance { get; set; }

    [StringLength(256)]
    public virtual string HousingAllowance { get; set; }

    [StringLength(256)]
    public virtual string OtherAllowance { get; set; }
}
