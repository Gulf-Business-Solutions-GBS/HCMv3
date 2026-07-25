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
/// JUDGMENT CALL: legacy also had a <c>DocAttachment</c> 1:1 child (auto-created in
/// <c>AfterConstruction</c>, used for attaching a certificate/document). The document-attachment module has
/// not been ported to HCMv3 yet, so this field is deliberately dropped as a simplification -- it can be
/// re-added once a DocAttachment-equivalent entity exists in this codebase.
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
}
