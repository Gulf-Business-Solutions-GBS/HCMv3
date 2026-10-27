using Abp.Application.Services.Dto;

namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TravelPerDiem"/>.
/// </summary>
public class TravelPerDiemDto : EntityDto<int>
{
    public int FromDestinationId { get; set; }

    public string FromDestinationNameEn { get; set; }

    public int ToDestinationId { get; set; }

    public string ToDestinationNameEn { get; set; }

    public int EmployeeGroupId { get; set; }

    public string EmployeeGroupNameEn { get; set; }

    public int EmployeeSubGroupId { get; set; }

    public string EmployeeSubGroupNameEn { get; set; }

    public double Amount { get; set; }
}
