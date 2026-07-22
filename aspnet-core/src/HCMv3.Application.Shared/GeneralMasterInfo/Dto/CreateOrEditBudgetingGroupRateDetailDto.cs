using System;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="BudgetingGroupRateDetail"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditBudgetingGroupRateDetailDto
{
    public int? Id { get; set; }

    public int? BudgetingGroupId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double BonusRate { get; set; }
}
