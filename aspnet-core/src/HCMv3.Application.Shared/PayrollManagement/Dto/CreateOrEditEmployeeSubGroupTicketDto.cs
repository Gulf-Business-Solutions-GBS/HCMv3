using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeSubGroupTicket"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditEmployeeSubGroupTicketDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeSubGroupId { get; set; }

    public int? TicketTypeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public int TicketCount { get; set; }

    public double MaxAmount { get; set; }

    public double FixAmount { get; set; }

    public FareType FareType { get; set; }

    public double AgeLimit { get; set; }

    public AccrualPeriod AccrualPeriod { get; set; }
}
