using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="TicketFare"/>. <c>Id == null</c> means create; otherwise
/// update. EndDate is always server-managed (auto-chained) and is not part of the input.
/// </summary>
public class CreateOrEditTicketFareDto
{
    public int? Id { get; set; }

    [Required]
    public int TicketRouteId { get; set; }

    [Required]
    public int TicketPaxTypeId { get; set; }

    [Required]
    public int TicketClassId { get; set; }

    /// <summary>"Effective Date" in the legacy UI.</summary>
    [Required]
    public DateTime StartDate { get; set; }

    public double OneWayTicketFare { get; set; }

    public double ReturnTicketFare { get; set; }
}
