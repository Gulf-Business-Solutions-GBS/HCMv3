using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Qualification/gender/marital/dependant-based allowance rule of a <see cref="SocialSecurity"/> master
/// (ported from legacy XAF <c>HCM10.Module.BusinessObjects.PayrollManagement.SocialSecurityRule</c>),
/// listed separately and linked back via <see cref="SocialSecurityId"/>.
///
/// The legacy <c>Gender</c> property's declared type
/// (<c>HCM10.Module.BusinessObjects.GeneralMasterInfo.Gender</c>) is, despite living in the
/// GeneralMasterInfo namespace, a plain C# <c>enum</c> (Male/Female) rather than an XAF persistent
/// lookup entity -- it maps directly onto this port's already-ported
/// <see cref="HCMv3.GeneralMasterInfo.Gender"/> enum, so no FK is needed here.
///
/// Unlike its <see cref="SocialSecurityDetail"/> sibling, the legacy source has NO date-range overlap
/// check for this entity (confirmed from the legacy <c>PerformValidation</c>) -- intentionally not added
/// here either; see <c>SocialSecurityRulesAppService</c>.
/// </summary>
public class SocialSecurityRule : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int SocialSecurityId { get; set; }

    public virtual SocialSecurity SocialSecurity { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual int QualificationId { get; set; }

    public virtual Qualification Qualification { get; set; }

    public virtual GeneralMasterInfo.Gender Gender { get; set; }

    public virtual int? MaritalStatusId { get; set; }

    public virtual MaritalStatus MaritalStatus { get; set; }

    public virtual int Dependant { get; set; }

    public virtual bool Disability { get; set; }

    public virtual double Amount { get; set; }
}
