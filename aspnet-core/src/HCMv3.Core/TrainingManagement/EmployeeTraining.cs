using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TrainingManagement;

/// <summary>
/// Links an Employee to a course + a specific scheduled run of it, tracking completion (ported from legacy
/// XAF <c>HCM10.Module.BusinessObjects.TrainingManagement.EmployeeTraining</c>). Legacy
/// <c>PerformValidation</c> parity (Employee/TrainingCourse/TrainingSchedule required, no duplicate
/// Employee+TrainingCourse+TrainingSchedule combination) is enforced in
/// <c>EmployeeTrainingsAppService</c>, not here.
///
/// Legacy's <c>DocAttachment</c> 1:1 child is replaced by the generic per-tenant attachment store
/// (attachment owner type <c>"EmployeeTraining"</c>), so it is not a column here. Legacy
/// <c>EmployeeCertification</c> is ported as <see cref="EmployeeCertificationId"/>; the picker offers only
/// the employee's certifications of type Training, as legacy's <c>TrainingCertificationList</c> did.
/// </summary>
public class EmployeeTraining : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int TrainingCourseId { get; set; }

    public virtual TrainingCourse TrainingCourse { get; set; }

    public virtual int TrainingScheduleId { get; set; }

    public virtual TrainingSchedule TrainingSchedule { get; set; }

    public virtual TrainingCompletionStatus CompletionStatus { get; set; }

    public virtual double Score { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    /// <summary>Legacy <c>EmployeeTraining.EmployeeCertification</c>: the certificate this training earned. Optional.</summary>
    public virtual int? EmployeeCertificationId { get; set; }

    public virtual EmployeeCertification EmployeeCertification { get; set; }
}
