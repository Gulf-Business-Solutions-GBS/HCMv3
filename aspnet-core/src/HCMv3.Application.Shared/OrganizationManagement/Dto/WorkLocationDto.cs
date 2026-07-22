using Abp.Application.Services.Dto;

namespace HCMv3.OrganizationManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="WorkLocation"/>.
/// </summary>
public class WorkLocationDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double GeoRange { get; set; }

    public string Address { get; set; }

    public string GeoFenceCoordinates { get; set; }

    public bool IsActive { get; set; }
}
