using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="Travel"/>.
/// </summary>
public class TravelDto : EntityDto<int>
{
    public string Code { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public int TravelTypeId { get; set; }

    public string TravelTypeNameEn { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double TotalTravelCost { get; set; }

    public string Comments { get; set; }
}
