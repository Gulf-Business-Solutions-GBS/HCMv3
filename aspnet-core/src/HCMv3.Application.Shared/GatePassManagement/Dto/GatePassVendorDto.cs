using Abp.Application.Services.Dto;

namespace HCMv3.GatePassManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="GatePassVendor"/>.
/// </summary>
public class GatePassVendorDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string VendorAddress { get; set; }
}
