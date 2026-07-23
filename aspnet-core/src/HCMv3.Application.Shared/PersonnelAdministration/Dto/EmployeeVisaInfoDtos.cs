using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeVisaInfoDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public string AuthorizedSign { get; set; }
    public string CurrentFileNo { get; set; }
    public string CurrentContractNo { get; set; }
    public string CurrentLicenseNo { get; set; }
    public string OldFileNo { get; set; }
    public string OldContractNo { get; set; }
    public string OldLicenseNo { get; set; }
    public string MOIFileNumber { get; set; }
    public string PreviousResidencyPeriod { get; set; }
    public string CurrentResidencyPeriod { get; set; }
    public double OldContractSalary { get; set; }
    public double CurrentContractSalary { get; set; }
    public string NewProfesion { get; set; }
    public string NewProfessionAr { get; set; }
    public string OldProfession { get; set; }
    public string OldCompany { get; set; }
    public string OldSponser { get; set; }
    public string TypeOfPassport { get; set; }
    public string IssuePlacePassport { get; set; }
    public DateTime PassportIssueDate { get; set; }
    public DateTime PassportExpiryDate { get; set; }
    public DateTime EntryDate { get; set; }
    public DateTime TerminationDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public long VisaNum { get; set; }
    public long ResidencyNum { get; set; }
    public int? PassportTypeId { get; set; }
    public string PassportTypeName { get; set; }
}

public class CreateOrEditEmployeeVisaInfoDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    public string AuthorizedSign { get; set; }
    public string CurrentFileNo { get; set; }
    public string CurrentContractNo { get; set; }
    public string CurrentLicenseNo { get; set; }
    public string OldFileNo { get; set; }
    public string OldContractNo { get; set; }
    public string OldLicenseNo { get; set; }
    public string MOIFileNumber { get; set; }
    public string PreviousResidencyPeriod { get; set; }
    public string CurrentResidencyPeriod { get; set; }
    public double OldContractSalary { get; set; }
    public double CurrentContractSalary { get; set; }
    public string NewProfesion { get; set; }
    public string NewProfessionAr { get; set; }
    public string OldProfession { get; set; }
    public string OldCompany { get; set; }
    public string OldSponser { get; set; }
    public string TypeOfPassport { get; set; }
    public string IssuePlacePassport { get; set; }
    public DateTime PassportIssueDate { get; set; }
    public DateTime PassportExpiryDate { get; set; }
    public DateTime EntryDate { get; set; }
    public DateTime TerminationDate { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public long VisaNum { get; set; }
    public long ResidencyNum { get; set; }
    public int? PassportTypeId { get; set; }
}

public class GetEmployeeVisaInfoForEditOutput
{
    public CreateOrEditEmployeeVisaInfoDto EmployeeVisaInfo { get; set; }
}

public class GetAllEmployeeVisaInfosInput : PagedAndSortedResultRequestDto, IShouldNormalize
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
