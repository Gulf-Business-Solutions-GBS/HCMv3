using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeCertificationDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Course { get; set; }
    public int InstitutionId { get; set; }
    public string InstitutionName { get; set; }
    public string Duration { get; set; }
    public string Certificate { get; set; }
    public string Grade { get; set; }
    public CertificationType CertificationType { get; set; }
}

public class CreateOrEditEmployeeCertificationDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    [StringLength(256)]
    public string Course { get; set; }

    [Required]
    public int InstitutionId { get; set; }

    [StringLength(64)]
    public string Duration { get; set; }

    [StringLength(256)]
    public string Certificate { get; set; }

    [StringLength(64)]
    public string Grade { get; set; }

    public CertificationType CertificationType { get; set; }
}

public class GetEmployeeCertificationForEditOutput
{
    public CreateOrEditEmployeeCertificationDto EmployeeCertification { get; set; }
}

public class GetAllEmployeeCertificationsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    /// <summary>Optional: only certifications of this type (the Employee Training picker asks for Training).</summary>
    public CertificationType? CertificationType { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}
