using System.ComponentModel.DataAnnotations;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="PersonalAction"/>.
/// <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditPersonalActionDto
{
    public int? Id { get; set; }

    /// <summary>
    /// Optional. When left blank the server auto-generates the next numeric code (legacy <c>GetMaxCode</c> parity).
    /// </summary>
    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameAr { get; set; }

    public bool ReqOrgAssignment { get; set; }

    public bool ReqBasicPay { get; set; }

    public bool ReqContract { get; set; }

    public bool ReqManager { get; set; }

    public bool ReqOnHold { get; set; }

    public bool ReqSeparation { get; set; }

    public bool ReqFnFPayment { get; set; }

    public bool IncludeInWorkflow { get; set; }

    public bool IsAttachmentRequired { get; set; }
}
