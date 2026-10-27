using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeSubGroupAssetAllowance"/>.
/// </summary>
public class EmployeeSubGroupAssetAllowanceDto : EntityDto<int>
{
    public int EmployeeSubGroupId { get; set; }

    public string EmployeeSubGroupName { get; set; }

    public int? WageTypeId { get; set; }

    public double Amount { get; set; }
}
