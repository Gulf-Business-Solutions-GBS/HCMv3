using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeGroup"/>.
/// </summary>
public class EmployeeGroupDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public double IndemnityMaxNumber { get; set; }

    public Unit IndemnityMaxUnit { get; set; }

    public double IndemnityMaxAccruePercentage { get; set; }
}
