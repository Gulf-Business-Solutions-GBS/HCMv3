using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TicketAccrualSummary"/>.
/// </summary>
public class TicketAccrualSummaryDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public double TicketAccrued { get; set; }

    public double TicketUsed { get; set; }
}
