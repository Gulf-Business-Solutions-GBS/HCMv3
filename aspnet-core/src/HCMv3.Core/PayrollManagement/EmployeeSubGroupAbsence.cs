using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Date-range leave quota row for an <see cref="EmployeeSubGroup"/> / <see cref="PayrollManagement.AbsenceType"/>
/// pair (ported from legacy XAF <c>HCM10.Module.BusinessObjects.PayrollManagement.EmployeeSubGroupAbsence</c>),
/// listed separately and linked back via <see cref="EmployeeSubGroupId"/>.
/// </summary>
public class EmployeeSubGroupAbsence : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeSubGroupId { get; set; }

    public virtual EmployeeSubGroup EmployeeSubGroup { get; set; }

    public virtual int AbsenceTypeId { get; set; }

    public virtual AbsenceType AbsenceType { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual double ChangeYear { get; set; }

    public virtual int TotalDays { get; set; }

    public virtual int MaxLimit { get; set; }

    public virtual int? AbsenceDeductionRuleId { get; set; }

    public virtual AbsenceDeductionRule AbsenceDeductionRule { get; set; }

    public virtual int MaxLimitReachNotificationInDays { get; set; }
}
