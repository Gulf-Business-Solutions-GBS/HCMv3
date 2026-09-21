---
helpId: sys.report-settings.edit
lang: en
title: Report Settings
entity: HCMv3.SystemSettings.ManageReportSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-report-settings/create-or-edit-manage-report-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageReportSettings]
---

## Overview

Settings for reports and payslips.

Only the two payroll sheet settings are read at present; the others have no effect.

There is one settings record per company; it cannot be deleted.

## ShowSubordinatesPayslip {#ManageReportSettings.ShowSubordinatesPayslip}
<!-- alias: loc:ShowSubordinatesPayslip, dom:ReportSettings_ShowSubordinatesPayslip -->

**What it is.** Whether managers can see their team's payslips.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## ShowPayslipOnDay {#ManageReportSettings.ShowPayslipOnDay}
<!-- alias: loc:ShowPayslipOnDay, dom:ReportSettings_ShowPayslipOnDay -->

**What it is.** The day of the month payslips become visible.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Whole number.

## ShowPayslipFromDate {#ManageReportSettings.ShowPayslipFromDate}
<!-- alias: loc:ShowPayslipFromDate, dom:ReportSettings_ShowPayslipFromDate -->

**What it is.** The earliest payslip employees can see.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Date value.

## ExportGridReportWithHeader {#ManageReportSettings.ExportGridReportWithHeader}
<!-- alias: loc:ExportGridReportWithHeader, dom:ReportSettings_ExportGridReportWithHeader -->

**What it is.** Whether exported reports include a header.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## ExportGridReportWithFooter {#ManageReportSettings.ExportGridReportWithFooter}
<!-- alias: loc:ExportGridReportWithFooter, dom:ReportSettings_ExportGridReportWithFooter -->

**What it is.** Whether exported reports include a footer.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## DecimalInReports {#ManageReportSettings.DecimalInReports}
<!-- alias: loc:DecimalInReports, dom:ReportSettings_DecimalInReports -->

**What it is.** The decimal places shown in reports.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Whole number.

## ShowPeriodInPayrollSheet {#ManageReportSettings.ShowPeriodInPayrollSheet}
<!-- alias: loc:ShowPeriodInPayrollSheet, dom:ReportSettings_ShowPeriodInPayrollSheet -->

**What it is.** Whether the payroll sheet report can cover a range of months.

**Why it matters.** Ticked, the report's To month and year are used, so it can run over several months. Unticked, it covers the From month only.

**Rules.** Yes or no.

## ShowPeriodInLeaveProvision {#ManageReportSettings.ShowPeriodInLeaveProvision}
<!-- alias: loc:ShowPeriodInLeaveProvision, dom:ReportSettings_ShowPeriodInLeaveProvision -->

**What it is.** Whether the leave provision report can cover a range of months.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## ShowPeriodInIndemnityProvision {#ManageReportSettings.ShowPeriodInIndemnityProvision}
<!-- alias: loc:ShowPeriodInIndemnityProvision, dom:ReportSettings_ShowPeriodInIndemnityProvision -->

**What it is.** Whether the indemnity provision report can cover a range of months.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## PeriodMonthLimitInPayrollSheet {#ManageReportSettings.PeriodMonthLimitInPayrollSheet}
<!-- alias: loc:PeriodMonthLimitInPayrollSheet, dom:ReportSettings_PeriodMonthLimitInPayrollSheet -->

**What it is.** The most months a payroll sheet report may cover.

**Example.** `12`

**Why it matters.** A longer range is refused. 0 means no limit.

**Rules.** Optional. Whole number.

## PeriodMonthLimitInLeaveProvision {#ManageReportSettings.PeriodMonthLimitInLeaveProvision}
<!-- alias: loc:PeriodMonthLimitInLeaveProvision, dom:ReportSettings_PeriodMonthLimitInLeaveProvision -->

**What it is.** The most months a leave provision report may cover.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Whole number.

## PeriodMonthLimitInIndemnityProvision {#ManageReportSettings.PeriodMonthLimitInIndemnityProvision}
<!-- alias: loc:PeriodMonthLimitInIndemnityProvision, dom:ReportSettings_PeriodMonthLimitInIndemnityProvision -->

**What it is.** The most months an indemnity provision report may cover.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Whole number.
