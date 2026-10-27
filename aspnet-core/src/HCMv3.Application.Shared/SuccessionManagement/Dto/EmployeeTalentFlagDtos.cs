using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.SuccessionManagement.Dto;

public class EmployeeTalentFlagDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int RiskOfLossId { get; set; }
    public string RiskOfLossNameEn { get; set; }
    public int ImpactOfLossId { get; set; }
    public string ImpactOfLossNameEn { get; set; }
    public int ReasonForLeavingId { get; set; }
    public string ReasonForLeavingNameEn { get; set; }
    public int TalentPoolId { get; set; }
    public string TalentPoolNameEn { get; set; }
}

public class CreateOrEditEmployeeTalentFlagDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int RiskOfLossId { get; set; }

    [Required]
    public int ImpactOfLossId { get; set; }

    [Required]
    public int ReasonForLeavingId { get; set; }

    [Required]
    public int TalentPoolId { get; set; }
}

public class GetEmployeeTalentFlagForEditOutput
{
    public CreateOrEditEmployeeTalentFlagDto EmployeeTalentFlag { get; set; }
}

public class GetAllEmployeeTalentFlagsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id DESC";
        }
    }
}
