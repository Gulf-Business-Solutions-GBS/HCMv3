using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeRewardDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public string RewardType { get; set; }
    public DateTime RewardDate { get; set; }
    public string Certificate { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeRewardDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    [StringLength(256)]
    public string RewardType { get; set; }

    [Required]
    public DateTime RewardDate { get; set; }

    [StringLength(256)]
    public string Certificate { get; set; }

    public string Comments { get; set; }
}

public class GetEmployeeRewardForEditOutput
{
    public CreateOrEditEmployeeRewardDto EmployeeReward { get; set; }
}

public class GetAllEmployeeRewardsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "RewardDate DESC";
        }
    }
}
