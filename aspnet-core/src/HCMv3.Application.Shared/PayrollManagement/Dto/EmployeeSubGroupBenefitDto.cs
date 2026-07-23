using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeSubGroupBenefit"/>.
/// </summary>
public class EmployeeSubGroupBenefitDto : EntityDto<int>
{
    public int EmployeeSubGroupId { get; set; }

    public string EmployeeSubGroupName { get; set; }

    public int BenefitTypeId { get; set; }

    public string BenefitTypeName { get; set; }

    public double MaxAmount { get; set; }
}
