using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="CostRegisterDetail"/>.
/// </summary>
public class CostRegisterDetailDto : EntityDto<int>
{
    public int CostRegisterId { get; set; }

    public int WageTypeId { get; set; }

    public string WageTypeName { get; set; }

    public int AccountInfoId { get; set; }

    public string AccountInfoName { get; set; }

    public int? CostElement1Id { get; set; }

    public int? CostElement2Id { get; set; }

    public int? CostElement3Id { get; set; }

    public int? CostElement4Id { get; set; }

    public int? CostElement5Id { get; set; }

    public int? CostElement6Id { get; set; }

    public int? CostElement7Id { get; set; }

    public double Amount { get; set; }

    public int? CurrencyId { get; set; }
}
