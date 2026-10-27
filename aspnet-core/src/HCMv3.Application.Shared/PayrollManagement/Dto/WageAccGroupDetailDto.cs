using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="WageAccGroupDetail"/>.
/// </summary>
public class WageAccGroupDetailDto : EntityDto<int>
{
    public int WageAccGroupId { get; set; }

    public string WageAccGroupName { get; set; }

    public int WageTypeId { get; set; }

    public string WageTypeName { get; set; }

    public int AccountInfoId { get; set; }

    public string AccountInfoName { get; set; }

    public OffsetAccountInfoType OffsetAccountInfoType { get; set; }

    public int? OffsetAccountInfoId { get; set; }

    public string OffsetAccountInfoName { get; set; }

    public int? CostElement1Id { get; set; }

    public int? CostElement2Id { get; set; }

    public int? CostElement3Id { get; set; }

    public int? CostElement4Id { get; set; }

    public int? CostElement5Id { get; set; }

    public int? CostElement6Id { get; set; }

    public int? CostElement7Id { get; set; }

    public double CostPercentage { get; set; }

    public bool MergeOffsetAccount { get; set; }
}
