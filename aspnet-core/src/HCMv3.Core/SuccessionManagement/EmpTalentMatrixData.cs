using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.SuccessionManagement;

/// <summary>
/// Time-bound nine-box talent matrix snapshot for an employee (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SuccessionManagement.EmpTalentMatrixData</c>). Legacy validation
/// (required fields, EndDate &gt;= StartDate, no overlapping StartDate/EndDate window for the same
/// Employee) is reproduced in <c>EmpTalentMatrixDatasAppService</c>.
/// </summary>
public class EmpTalentMatrixData : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual int PerformanceRatingId { get; set; }

    public virtual NineBoxRating PerformanceRating { get; set; }

    public virtual int PotentialRatingId { get; set; }

    public virtual NineBoxRating PotentialRating { get; set; }

    public virtual int OverallRatingId { get; set; }

    public virtual NineBoxRating OverallRating { get; set; }

    public virtual int CalibratedRatingId { get; set; }

    public virtual NineBoxRating CalibratedRating { get; set; }
}
