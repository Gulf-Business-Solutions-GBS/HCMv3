using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="TicketPaxType"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditTicketPaxTypeDto
{
    public int? Id { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameAr { get; set; }

    public double AgeFrom { get; set; }

    public double AgeTo { get; set; }
}
