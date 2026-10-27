using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TempEmployeeBenefit"/>.
/// </summary>
public class TempEmployeeBenefitDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; }

    public int TempBenefitTypeId { get; set; }

    public string TempBenefitTypeGrade { get; set; }
}
