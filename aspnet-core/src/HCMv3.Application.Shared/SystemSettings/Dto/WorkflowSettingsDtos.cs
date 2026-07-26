using Abp.Application.Services.Dto;

namespace HCMv3.SystemSettings.Dto;

public class WorkflowSettingsListDto : EntityDto
{
    public bool EnableWorkflowGroup { get; set; }

    public bool EnableOrgUnitTypeInWorkflow { get; set; }

    public bool HideLeavePayDetailsInWorkflow { get; set; }

    public int EnableTravelCostOnWorkflowstep { get; set; }

    public int OvertimeRequestModificationStep { get; set; }

    public bool DynamicWorkflowSteps { get; set; }

    public bool SendBackToCreator { get; set; }

    public int? DelegateEmployeeListByOrganizationUnitTypeId { get; set; }
}

public class WorkflowSettingsEditDto : EntityDto
{
    public bool EnableWorkflowGroup { get; set; }

    public bool EnableOrgUnitTypeInWorkflow { get; set; }

    public bool HideLeavePayDetailsInWorkflow { get; set; }

    public int EnableTravelCostOnWorkflowstep { get; set; }

    public int OvertimeRequestModificationStep { get; set; }

    public bool DynamicWorkflowSteps { get; set; }

    public bool SendBackToCreator { get; set; }

    public int? DelegateEmployeeListByOrganizationUnitTypeId { get; set; }
}
