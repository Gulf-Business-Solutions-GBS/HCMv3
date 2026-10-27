using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="PersonalAction"/>.
/// </summary>
public class PersonalActionDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

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
