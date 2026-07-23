using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeEducationDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int EducationId { get; set; }
    public string EducationName { get; set; }
    public int EducationBranchId { get; set; }
    public string EducationBranchName { get; set; }
    public int? UniversityId { get; set; }
    public string UniversityName { get; set; }
    public int? InstitutionId { get; set; }
    public string InstitutionName { get; set; }
    public int? CountryId { get; set; }
    public string CountryName { get; set; }
    public string Location { get; set; }
    public double DurationOfCourse { get; set; }
    public string Grade { get; set; }
    public string YearOfPassing { get; set; }
}

public class CreateOrEditEmployeeEducationDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required]
    public int EducationId { get; set; }

    [Required]
    public int EducationBranchId { get; set; }

    public int? UniversityId { get; set; }
    public int? InstitutionId { get; set; }
    public int? CountryId { get; set; }

    [StringLength(256)]
    public string Location { get; set; }

    public double DurationOfCourse { get; set; }

    [StringLength(64)]
    public string Grade { get; set; }

    [StringLength(16)]
    public string YearOfPassing { get; set; }
}

public class GetEmployeeEducationForEditOutput
{
    public CreateOrEditEmployeeEducationDto EmployeeEducation { get; set; }
}

public class GetAllEmployeeEducationsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}
