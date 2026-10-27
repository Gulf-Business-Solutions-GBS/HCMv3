using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="LeaveAccrualSummary"/>.
/// </summary>
public class LeaveAccrualSummaryDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public int AbsenceTypeId { get; set; }

    public string AbsenceTypeName { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public double AccrualDays { get; set; }

    public double UsedDays { get; set; }

    public double CumulativeAccruedDays { get; set; }
}
