using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeSubGroupTicket"/>.
/// </summary>
public class EmployeeSubGroupTicketDto : EntityDto<int>
{
    public int EmployeeSubGroupId { get; set; }

    public string EmployeeSubGroupName { get; set; }

    public int? TicketTypeId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int TicketCount { get; set; }

    public double MaxAmount { get; set; }

    public double FixAmount { get; set; }

    public FareType FareType { get; set; }

    public double AgeLimit { get; set; }

    public AccrualPeriod AccrualPeriod { get; set; }
}
