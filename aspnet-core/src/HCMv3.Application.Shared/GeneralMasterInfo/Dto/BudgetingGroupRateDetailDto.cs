using System;
using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="BudgetingGroupRateDetail"/>.
/// </summary>
public class BudgetingGroupRateDetailDto : EntityDto<int>
{
    public int? BudgetingGroupId { get; set; }

    public string BudgetingGroupName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double BonusRate { get; set; }
}
