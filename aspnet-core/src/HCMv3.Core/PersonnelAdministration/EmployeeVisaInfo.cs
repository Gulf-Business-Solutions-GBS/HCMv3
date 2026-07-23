using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee visa/residency record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeVisaInfo</c>).
/// </summary>
public class EmployeeVisaInfo : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    [StringLength(128)]
    public virtual string AuthorizedSign { get; set; }

    [StringLength(64)]
    public virtual string CurrentFileNo { get; set; }

    [StringLength(64)]
    public virtual string CurrentContractNo { get; set; }

    [StringLength(64)]
    public virtual string CurrentLicenseNo { get; set; }

    [StringLength(64)]
    public virtual string OldFileNo { get; set; }

    [StringLength(64)]
    public virtual string OldContractNo { get; set; }

    [StringLength(64)]
    public virtual string OldLicenseNo { get; set; }

    [StringLength(64)]
    public virtual string MOIFileNumber { get; set; }

    [StringLength(64)]
    public virtual string PreviousResidencyPeriod { get; set; }

    [StringLength(64)]
    public virtual string CurrentResidencyPeriod { get; set; }

    public virtual double OldContractSalary { get; set; }

    public virtual double CurrentContractSalary { get; set; }

    [StringLength(256)]
    public virtual string NewProfesion { get; set; }

    [StringLength(256)]
    public virtual string NewProfessionAr { get; set; }

    [StringLength(256)]
    public virtual string OldProfession { get; set; }

    [StringLength(256)]
    public virtual string OldCompany { get; set; }

    [StringLength(256)]
    public virtual string OldSponser { get; set; }

    [StringLength(64)]
    public virtual string TypeOfPassport { get; set; }

    [StringLength(128)]
    public virtual string IssuePlacePassport { get; set; }

    public virtual DateTime PassportIssueDate { get; set; }

    public virtual DateTime PassportExpiryDate { get; set; }

    public virtual DateTime EntryDate { get; set; }

    public virtual DateTime TerminationDate { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual long VisaNum { get; set; }

    public virtual long ResidencyNum { get; set; }

    public virtual int? PassportTypeId { get; set; }

    public virtual PassportType PassportType { get; set; }
}
