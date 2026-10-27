using System.ComponentModel.DataAnnotations;

namespace HCMv3.Licensing.Dto;

/// <summary>Tick or untick one module for one user.</summary>
public class SetUserModuleLicenceInput
{
    [Range(1, long.MaxValue)]
    public long UserId { get; set; }

    [EnumDataType(typeof(LicenceModule))]
    public LicenceModule Module { get; set; }

    public bool IsAssigned { get; set; }
}
