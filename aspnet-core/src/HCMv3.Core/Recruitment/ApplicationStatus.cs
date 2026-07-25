using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Recruitment;

/// <summary>
/// An applicant's application against a vacancy, tracking status through the hiring pipeline (ported from
/// legacy XAF <c>HCM10.Module.BusinessObjects.Recruitment.ApplicationStatus</c>). Legacy
/// <c>SaveToEmployee</c> (auto-create an <c>Employee</c> record when status becomes <see cref="Recruitment
/// .AppStatus.Joined"/>) is ported to <c>ApplicationStatusesAppService</c>, not here.
/// </summary>
public class ApplicationStatus : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int? VacancyId { get; set; }
    public virtual Vacancy Vacancy { get; set; }

    public virtual int? ApplicantCVId { get; set; }
    public virtual ApplicantCV ApplicantCV { get; set; }

    public virtual AppStatus AppStatus { get; set; }

    [StringLength(4000)]
    public virtual string CoverLetter { get; set; }
}
