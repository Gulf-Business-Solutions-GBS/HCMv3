using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.JobProfileManagement;

/// <summary>
/// Employee acknowledgement of a <see cref="JobDescription"/> version (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.JobProfileManagement.JDAcknowledgement</c>). Legacy
/// <c>PerformValidation</c> parity (Employee/JobTitle/JobDescription required; no duplicate
/// Employee+JobTitle+JobDescription combination) is enforced in
/// <c>JobDescriptionAcknowledgementsAppService</c>, not here.
/// </summary>
public class JobDescriptionAcknowledgement : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int JobTitleId { get; set; }

    public virtual JobTitle JobTitle { get; set; }

    public virtual int JobDescriptionId { get; set; }

    public virtual JobDescription JobDescription { get; set; }

    public virtual string Version { get; set; }

    public virtual bool Acknowledged { get; set; }
}
