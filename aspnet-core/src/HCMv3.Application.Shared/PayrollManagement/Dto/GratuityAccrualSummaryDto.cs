using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="GratuityAccrualSummary"/>.
/// </summary>
public class GratuityAccrualSummaryDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public double GratuityDaysAccrued { get; set; }

    public double GratuityDaysBalance { get; set; }

    public double GratuityAmount { get; set; }

    public double GratuityAmountPaid { get; set; }
}
