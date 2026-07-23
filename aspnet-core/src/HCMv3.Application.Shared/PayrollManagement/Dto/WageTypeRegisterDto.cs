using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="WageTypeRegister"/>.
/// </summary>
public class WageTypeRegisterDto : EntityDto<int>
{
    public int? EmployeeId { get; set; }

    public string EmployeeName { get; set; }

    public int ForPeriod { get; set; }

    public int InPeriod { get; set; }
}
