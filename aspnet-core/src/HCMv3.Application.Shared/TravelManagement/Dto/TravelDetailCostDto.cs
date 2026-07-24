using Abp.Application.Services.Dto;

namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TravelDetailCost"/>.
/// </summary>
public class TravelDetailCostDto : EntityDto<int>
{
    public int TravelDetailId { get; set; }

    public int ExpenseTypeId { get; set; }

    public string ExpenseTypeNameEn { get; set; }

    public double Amount { get; set; }

    public string Comments { get; set; }
}
