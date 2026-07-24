using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee health reimbursement claim record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeHealthReimburse</c>, Wave 4 deferred
/// bucket).
///
/// DELIBERATE SIMPLIFICATION: the legacy's extensive <c>CheckQuotaBalance</c>/<c>GetEligibleHealthReimburseAmount</c>/
/// <c>GetCarryForwardAmount</c> quota-enforcement logic depends on <c>EmployeeSubGroup.HealthReimburse</c>
/// (salary-profile) and <c>EmployeeDependant</c>/<c>EmployeeDependantException</c>/<c>EmployeeBenefitException</c>
/// (this wave's own entities), which cross the ManpowerPlanning/Payroll salary-profile module boundary (not
/// yet ported) -- NOT ported on the entity itself, consistent with every other cross-module cascade
/// omission in this port. The legacy <c>DocAttachment</c> FK is dropped.
/// </summary>
public class EmployeeHealthReimburse : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime PaymentDate { get; set; }

    public virtual double Amount { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
