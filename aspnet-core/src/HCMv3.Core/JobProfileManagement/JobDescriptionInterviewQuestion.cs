using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.JobProfileManagement;

/// <summary>
/// Aggregated child of <see cref="JobDescription"/> linking it to an <see cref="InterviewQuestion"/>
/// (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.JobProfileManagement.JDInterviewQuestion</c>). Legacy
/// <c>PerformValidation</c> parity (no duplicate InterviewQuestion for the same JobDescription) is
/// enforced in <c>JobDescriptionInterviewQuestionsAppService</c>, not here.
/// </summary>
public class JobDescriptionInterviewQuestion : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int JobDescriptionId { get; set; }

    public virtual JobDescription JobDescription { get; set; }

    public virtual int InterviewQuestionId { get; set; }

    public virtual InterviewQuestion InterviewQuestion { get; set; }
}
