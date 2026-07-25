using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;

namespace HCMv3.ManpowerPlanningManagement;

/// <summary>
/// Position/vacancy master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ManpowerPlanningManagement.Position</c>).
///
/// DELIBERATE SIMPLIFICATION: the legacy entity's 7 discrete cascading-dropdown <c>OrgLevel1</c>..
/// <c>OrgLevel7</c> fields are collapsed into a single <see cref="OrganizationUnitId"/> FK to Zero's own
/// built-in, already hierarchical <c>Abp.Organizations.OrganizationUnit</c> tree -- the identical
/// simplification already applied to <c>HCMv3.PersonnelAdministration.EmployeeOrgAssignment</c> (which had
/// deliberately deferred this whole module's FK). The legacy <c>CodeWithNameEn</c> computed
/// <c>PersistentAlias</c> is not persisted here; callers can compute it from <see cref="Code"/> +
/// <see cref="NameEn"/> (see <c>PositionDto.CodeWithNameEn</c>).
/// </summary>
public class Position : FullAuditedEntity, IMustHaveTenant
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

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual bool Vacant { get; set; }

    public virtual bool Active { get; set; }

    public virtual int? JobTitleId { get; set; }

    public virtual JobTitle JobTitle { get; set; }

    public virtual int? EmployeeGroupId { get; set; }

    public virtual EmployeeGroup EmployeeGroup { get; set; }

    public virtual int? EmployeeSubGroupId { get; set; }

    public virtual EmployeeSubGroup EmployeeSubGroup { get; set; }

    public virtual int? PayrollAreaId { get; set; }

    public virtual PayrollArea PayrollArea { get; set; }

    public virtual int? WorkLocationId { get; set; }

    public virtual WorkLocation WorkLocation { get; set; }

    public virtual int? CostCenterId { get; set; }

    public virtual CostCenter CostCenter { get; set; }

    /// <summary>
    /// FK to Zero's own <c>Abp.Organizations.OrganizationUnit</c> tree -- replaces the legacy's 7 discrete
    /// OrgLevel1..7 fields (see class remarks).
    /// </summary>
    public virtual long? OrganizationUnitId { get; set; }

    public virtual Abp.Organizations.OrganizationUnit OrganizationUnit { get; set; }

    public virtual double MinSalary { get; set; }

    public virtual double MidSalary { get; set; }

    public virtual double MaxSalary { get; set; }

    public virtual bool IsBudgeted { get; set; }

    /// <summary>
    /// Maximum number of employees that may simultaneously hold this position (legacy comment: "Added to
    /// allow single position among multiple employees at the same time").
    /// </summary>
    public virtual int FTELimit { get; set; }

    public virtual ICollection<PositionRelationship> PositionRelationships { get; set; }
}
