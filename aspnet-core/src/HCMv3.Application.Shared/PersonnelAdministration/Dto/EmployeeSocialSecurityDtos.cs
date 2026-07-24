using System;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeSocialSecurityDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public long SocialSecurityNum { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool SocialSecurityContrib { get; set; }
}

public class CreateOrEditEmployeeSocialSecurityDto
{
    public int? Id { get; set; }

    public int EmployeeId { get; set; }

    public long SocialSecurityNum { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool SocialSecurityContrib { get; set; }
}

public class GetEmployeeSocialSecurityForEditOutput
{
    public CreateOrEditEmployeeSocialSecurityDto EmployeeSocialSecurity { get; set; }
}

public class GetAllEmployeeSocialSecuritiesInput : PagedAndSortedResultRequestDto, IShouldNormalize
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
