using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSettings;

/// <summary>
/// Tenant-wide report configuration (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManageReportSettings</c>). Legacy <c>OnDeleting</c>
/// unconditionally throws -- enforced in <c>ManageReportSettingsAppService.DeleteReportSettings</c>, not here.
/// Legacy <c>PerformValidation</c> was empty (no real business rule to replicate).
/// </summary>
public class ManageReportSettings : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual bool ShowSubordinatesPayslip { get; set; }

    public virtual bool ExportGridReportWithHeader { get; set; }

    public virtual bool ExportGridReportWithFooter { get; set; }

    public virtual int ShowPayslipOnDay { get; set; }

    public virtual DateTime ShowPayslipFromDate { get; set; }

    public virtual int DecimalInReports { get; set; }

    public virtual bool ShowPeriodInPayrollSheet { get; set; }

    public virtual bool ShowPeriodInLeaveProvision { get; set; }

    public virtual bool ShowPeriodInIndemnityProvision { get; set; }

    public virtual int PeriodMonthLimitInPayrollSheet { get; set; }

    public virtual int PeriodMonthLimitInLeaveProvision { get; set; }

    public virtual int PeriodMonthLimitInIndemnityProvision { get; set; }
}
