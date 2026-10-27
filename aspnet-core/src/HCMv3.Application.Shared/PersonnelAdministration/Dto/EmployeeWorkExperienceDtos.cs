using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeWorkExperienceDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Company { get; set; }
    public string Position { get; set; }
    public string Location { get; set; }
    public string ReasonForLeaving { get; set; }
}

public class CreateOrEditEmployeeWorkExperienceDto
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
    public string Company { get; set; }

    [StringLength(256)]
    public string Position { get; set; }

    [StringLength(256)]
    public string Location { get; set; }

    [StringLength(1000)]
    public string ReasonForLeaving { get; set; }
}

public class GetEmployeeWorkExperienceForEditOutput
{
    public CreateOrEditEmployeeWorkExperienceDto EmployeeWorkExperience { get; set; }
}

public class GetAllEmployeeWorkExperiencesInput : PagedAndSortedResultRequestDto, IShouldNormalize
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
