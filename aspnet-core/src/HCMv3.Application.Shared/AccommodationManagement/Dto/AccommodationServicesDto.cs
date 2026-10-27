using System;
using Abp.Application.Services.Dto;

namespace HCMv3.AccommodationManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="AccommodationServices"/>.
/// </summary>
public class AccommodationServicesDto : EntityDto<int>
{
    public int EmployeeAccommodationId { get; set; }

    public int AccommodationServiceTypeId { get; set; }

    public string AccommodationServiceTypeName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Comments { get; set; }
}
