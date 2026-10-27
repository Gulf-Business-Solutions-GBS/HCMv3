using System;
using Abp.Application.Services.Dto;

namespace HCMv3.GatePassManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="GatePass"/>.
/// </summary>
public class GatePassDto : EntityDto<int>
{
    public int GatePassNumber { get; set; }

    public int GatePassTypeId { get; set; }

    public string GatePassTypeNameEn { get; set; }

    public int GatePassMovementId { get; set; }

    public string MovementNameEn { get; set; }

    public int? LocationId { get; set; }

    public string LocationNameEn { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime ValidUpto { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int? RequestedByEmployeeId { get; set; }

    public string RequestedByFullName { get; set; }

    public int? GatePassStatusId { get; set; }

    public string StatusNameEn { get; set; }

    public string Comments { get; set; }
}
