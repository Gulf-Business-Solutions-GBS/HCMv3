using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// NumberSeries (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.NumberSeries</c>).
/// Pure Code/Name/Start/Prefix/Formatting record -- no NameEn/NameAr (confirmed against the legacy source).
/// <see cref="EmploymentType"/> says which employees the series numbers: one series per employment type, or a
/// single <c>ALL</c> series for everyone (uniqueness rules in <c>NumberSeriesAppService</c>, legacy
/// <c>PerformValidation</c>). Employee codes are generated from it by
/// <c>PersonnelAdministration.Services.EmployeeCodeGenerator</c> (legacy <c>NumberSeries.GetCode</c>).
/// </summary>
public class NumberSeries : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string Name { get; set; }

    public virtual long Start { get; set; }

    [StringLength(64)]
    public virtual string Prefix { get; set; }

    [Required]
    [StringLength(64)]
    public virtual string Formatting { get; set; }

    /// <summary>Legacy <c>NumberSeries.EmploymentType</c>. <c>ALL</c> = the one series used for every employee.</summary>
    public virtual EmploymentType EmploymentType { get; set; }
}
