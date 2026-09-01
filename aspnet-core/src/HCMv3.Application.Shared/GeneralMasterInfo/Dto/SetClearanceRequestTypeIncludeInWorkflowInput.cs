using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Turns self-service exit clearance on or off for one clearance type. Separate from the shared lookup
/// create/edit DTO because that DTO is generic across every Code/NameEn/NameAr lookup in the module, and
/// this flag belongs to exactly one of them.
/// </summary>
public class SetClearanceRequestTypeIncludeInWorkflowInput : EntityDto<int>
{
    public bool IncludeInWorkflow { get; set; }
}
