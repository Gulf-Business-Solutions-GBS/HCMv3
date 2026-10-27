using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Date-range, years-of-service-tiered gratuity accrual rule for an <see cref="EmployeeSubGroup"/> (ported
/// from legacy XAF <c>HCM10.Module.BusinessObjects.PayrollManagement.EmployeeSubGroupGratuity</c>), listed
/// separately and linked back via <see cref="EmployeeSubGroupId"/>.
/// </summary>
public class EmployeeSubGroupGratuity : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeSubGroupId { get; set; }

    public virtual EmployeeSubGroup EmployeeSubGroup { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual double ChangeYear { get; set; }

    public virtual double GratuityDays { get; set; }

    public virtual double BasisDays { get; set; }

    public virtual bool ImpactFromDateOfJoining { get; set; }
}
