using System.ComponentModel.DataAnnotations;

namespace HCMv3.GatePassManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="GatePassVendor"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditGatePassVendorDto
{
    public int? Id { get; set; }

    [StringLength(64)]
    public string Code { get; set; }

    [Required]
    [StringLength(256)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(256)]
    public string NameAr { get; set; }

    [StringLength(1000)]
    public string VendorAddress { get; set; }
}
