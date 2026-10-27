using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TicketFare"/>.
/// </summary>
public class TicketFareDto : EntityDto<int>
{
    public int TicketRouteId { get; set; }

    public string TicketRouteName { get; set; }

    public int TicketPaxTypeId { get; set; }

    public string TicketPaxTypeName { get; set; }

    public int TicketClassId { get; set; }

    public string TicketClassName { get; set; }

    /// <summary>"Effective Date" in the legacy UI.</summary>
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double OneWayTicketFare { get; set; }

    public double ReturnTicketFare { get; set; }
}
