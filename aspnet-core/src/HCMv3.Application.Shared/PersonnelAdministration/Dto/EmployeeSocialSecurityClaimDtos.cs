using System;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeSocialSecurityClaimDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public DateTime ClaimDate { get; set; }
    public double EmployeeContributionAmount { get; set; }
    public double EmployerContributionAmount { get; set; }
}

public class CreateOrEditEmployeeSocialSecurityClaimDto
{
    public int? Id { get; set; }

    public int EmployeeId { get; set; }

    public DateTime ClaimDate { get; set; }

    public double EmployeeContributionAmount { get; set; }

    public double EmployerContributionAmount { get; set; }
}

public class GetEmployeeSocialSecurityClaimForEditOutput
{
    public CreateOrEditEmployeeSocialSecurityClaimDto EmployeeSocialSecurityClaim { get; set; }
}

public class GetAllEmployeeSocialSecurityClaimsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "ClaimDate DESC";
        }
    }
}
