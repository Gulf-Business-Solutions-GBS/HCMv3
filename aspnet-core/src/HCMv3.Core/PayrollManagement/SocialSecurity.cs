using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Social Security master/header (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.SocialSecurity</c>). The legacy's four Aggregated
/// XPCollection children (<c>SocialSecurityDetail</c>, <c>SocialSecurityRule</c>,
/// <c>SocialSecurityMaritalAllowance</c>/<c>EducationAllowance</c>) are ported as separate top-level
/// entities (<see cref="SocialSecurityDetail"/>, <see cref="SocialSecurityRule"/>,
/// <see cref="SocialSecurityMaritalAllowance"/>, <see cref="SocialSecurityEducationAllowance"/>), each
/// FK'd back via a <c>SocialSecurityId</c>, per this port's flat-entity convention (matching e.g.
/// <c>AbsenceDeductionRule</c>/<c>AbsenceDedRuleDetail</c>) rather than the legacy's owned/aggregated
/// collection shape. The legacy <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
/// </summary>
public class SocialSecurity : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int NationalityId { get; set; }

    public virtual Nationality Nationality { get; set; }

    public virtual double EarningsCap { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual double ChildAllowance { get; set; }

    public virtual double SpecialNeedChildAllowance { get; set; }
}
