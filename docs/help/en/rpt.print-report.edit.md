---
helpId: rpt.print-report.edit
lang: en
title: Print Reports — Create / Edit
entity: HCMv3.ReportManagement.PrintReport
surfaces:
  - angular/src/app/admin/report-management/print-reports/create-or-edit-print-report-modal.component.html
audience: [admin]
permissions: [Pages.ReportManagement.PrintReports.Create]
---

## Overview

One report in the Print Reports list. Each company is given the standard list when it is created; a report you rename, switch off or delete is left as you set it. A report can be run only if the product has that report built in -- otherwise it is listed but cannot be run.

## ReportName {#PrintReport.ReportName}
<!-- alias: loc:ReportName, dom:PrintReport_ReportName -->

**What it is.** The name of the built-in report this entry runs.

**Why it matters.** Must match a built-in report exactly, or the entry cannot be run. Change it only to point at another built-in report.

**Rules.** Optional. Maximum 256 characters.

## ReportCategory {#PrintReport.ReportCategory}
<!-- alias: loc:ReportCategory, dom:PrintReport_ReportCategory -->

**What it is.** Whether this is a standard or a custom report.

**Why it matters.** Shown as a column in the Print Reports list; it changes nothing else.

**Rules.** Required. One of: Default, Custom.

## CustomPercentage {#PrintReport.CustomPercentage}
<!-- alias: loc:CustomPercentage, dom:PrintReport_CustomPercentage -->

**What it is.** How far the report has been customised, as a percentage.

**Why it matters.** Not read at present.

**Rules.** Optional. Number.

## ReportCreatedOn {#PrintReport.ReportCreatedOn}
<!-- alias: loc:ReportCreatedOn, dom:PrintReport_ReportCreatedOn -->

**What it is.** When the report was made.

**Why it matters.** Not read at present.

**Rules.** Optional. Date value.

## IsActive {#PrintReport.IsActive}
<!-- alias: loc:IsActive, dom:PrintReport_IsActive -->

**What it is.** Whether the report is listed.

**Why it matters.** Unticked reports are left out of the Print Reports list for everyone.

**Rules.** Yes or no.
