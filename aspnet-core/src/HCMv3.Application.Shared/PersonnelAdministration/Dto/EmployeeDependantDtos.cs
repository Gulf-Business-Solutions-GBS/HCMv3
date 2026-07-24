using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeDependantDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public string NameEn { get; set; }
    public string NameAr { get; set; }
    public int DependantTypeId { get; set; }
    public string DependantTypeName { get; set; }
    public DateTime BirthDate { get; set; }
    public int TicketTypeId { get; set; }
    public string TicketTypeName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool SpecialNeed { get; set; }
    public bool IncludeInSocialSecurity { get; set; }
    public bool IncludeInHealthInsurance { get; set; }
}

public class CreateOrEditEmployeeDependantDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    [StringLength(256)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(256)]
    public string NameAr { get; set; }

    [Required]
    public int DependantTypeId { get; set; }

    [Required]
    public DateTime BirthDate { get; set; }

    [Required]
    public int TicketTypeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public bool SpecialNeed { get; set; }

    public bool IncludeInSocialSecurity { get; set; }

    public bool IncludeInHealthInsurance { get; set; }
}

public class GetEmployeeDependantForEditOutput
{
    public CreateOrEditEmployeeDependantDto EmployeeDependant { get; set; }
}

public class GetAllEmployeeDependantsInput : PagedAndSortedResultRequestDto, IShouldNormalize
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
