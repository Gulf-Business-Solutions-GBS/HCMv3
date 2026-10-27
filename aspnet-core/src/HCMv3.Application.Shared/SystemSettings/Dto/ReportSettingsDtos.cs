using System;
using Abp.Application.Services.Dto;

namespace HCMv3.SystemSettings.Dto;

public class ReportSettingsListDto : EntityDto
{
    public bool ShowSubordinatesPayslip { get; set; }

    public bool ExportGridReportWithHeader { get; set; }

    public bool ExportGridReportWithFooter { get; set; }

    public int ShowPayslipOnDay { get; set; }

    public DateTime ShowPayslipFromDate { get; set; }

    public int DecimalInReports { get; set; }

    public bool ShowPeriodInPayrollSheet { get; set; }

    public bool ShowPeriodInLeaveProvision { get; set; }

    public bool ShowPeriodInIndemnityProvision { get; set; }

    public int PeriodMonthLimitInPayrollSheet { get; set; }

    public int PeriodMonthLimitInLeaveProvision { get; set; }

    public int PeriodMonthLimitInIndemnityProvision { get; set; }
}

public class ReportSettingsEditDto : EntityDto
{
    public bool ShowSubordinatesPayslip { get; set; }

    public bool ExportGridReportWithHeader { get; set; }

    public bool ExportGridReportWithFooter { get; set; }

    public int ShowPayslipOnDay { get; set; }

    public DateTime ShowPayslipFromDate { get; set; }

    public int DecimalInReports { get; set; }

    public bool ShowPeriodInPayrollSheet { get; set; }

    public bool ShowPeriodInLeaveProvision { get; set; }

    public bool ShowPeriodInIndemnityProvision { get; set; }

    public int PeriodMonthLimitInPayrollSheet { get; set; }

    public int PeriodMonthLimitInLeaveProvision { get; set; }

    public int PeriodMonthLimitInIndemnityProvision { get; set; }
}
