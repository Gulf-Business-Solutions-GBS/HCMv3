using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSettings;

/// <summary>
/// Tenant-wide workflow configuration (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManageWorkflowSettings</c>). Legacy <c>OnDeleting</c>
/// unconditionally throws -- enforced in <c>ManageWorkflowSettingsAppService.DeleteWorkflowSettings</c>, not
/// here. Legacy <c>DelegateEmployeeListBy</c> referenced <c>OrganizationManagement.OrganizationUnitType</c> by
/// navigation property; ported here as a plain FK id (<see cref="DelegateEmployeeListByOrganizationUnitTypeId"/>)
/// following this codebase's flat-FK convention (see <c>PersonnelAdministration.Employee.NationalityId</c>).
/// </summary>
public class ManageWorkflowSettings : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual bool EnableWorkflowGroup { get; set; }

    public virtual bool EnableOrgUnitTypeInWorkflow { get; set; }

    public virtual bool HideLeavePayDetailsInWorkflow { get; set; }

    public virtual int EnableTravelCostOnWorkflowstep { get; set; }

    public virtual int OvertimeRequestModificationStep { get; set; }

    public virtual bool DynamicWorkflowSteps { get; set; }

    public virtual bool SendBackToCreator { get; set; }

    public virtual int? DelegateEmployeeListByOrganizationUnitTypeId { get; set; }
}
