using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeSponsorDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int SponsorId { get; set; }
    public string SponsorNameEn { get; set; }
    public int GovtJobTitleId { get; set; }
    public string GovtJobTitleNameEn { get; set; }
    public string EmpSponsorRelation { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public double ContractSalary { get; set; }
    public int SalaryCurrencyId { get; set; }
    public string SalaryCurrencyNameEn { get; set; }
    public ResidencyType ResidencyType { get; set; }
    public int WorkVisaTypeId { get; set; }
    public string WorkVisaTypeNameEn { get; set; }
}

public class CreateOrEditEmployeeSponsorDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int SponsorId { get; set; }

    [Required]
    public int GovtJobTitleId { get; set; }

    [StringLength(256)]
    public string EmpSponsorRelation { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public double ContractSalary { get; set; }

    [Required]
    public int SalaryCurrencyId { get; set; }

    public ResidencyType ResidencyType { get; set; }

    [Required]
    public int WorkVisaTypeId { get; set; }
}

public class GetEmployeeSponsorForEditOutput
{
    public CreateOrEditEmployeeSponsorDto EmployeeSponsor { get; set; }
}

public class GetAllEmployeeSponsorsInput : PagedAndSortedResultRequestDto, IShouldNormalize
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
