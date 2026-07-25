using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.SuccessionManagement;

/// <summary>
/// Time-bound potential-assessment score for an employee (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SuccessionManagement.EmpPotentialAssessment</c>). Legacy validation
/// (required fields, EndDate &gt;= StartDate, no overlapping StartDate/EndDate window for the same
/// Employee) is reproduced in <c>EmpPotentialAssessmentsAppService</c>.
/// </summary>
public class EmpPotentialAssessment : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    [StringLength(2000)]
    public virtual string Description { get; set; }

    public virtual double Score { get; set; }

    public virtual int RatingId { get; set; }

    public virtual SuccessionRating Rating { get; set; }

    public virtual ScoreSource Source { get; set; }

    public virtual int NineBoxRatingId { get; set; }

    public virtual NineBoxRating NineBoxRating { get; set; }
}
