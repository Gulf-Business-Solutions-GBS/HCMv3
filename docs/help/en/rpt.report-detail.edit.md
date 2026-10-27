---
helpId: rpt.report-detail.edit
lang: en
title: Report Details — Create / Edit
entity: HCMv3.ReportManagement.ReportDetail
surfaces:
  - angular/src/app/admin/report-management/report-details/create-or-edit-report-detail-modal.component.html
audience: [admin]
permissions: [Pages.ReportManagement.ReportDetails.Create]
---

## Overview

One level of a drill-down report. Part of the report designer carried over from the old system. Not read at present: nothing in HCMv3 runs these definitions, so records here have no effect.

## ReportMaster {#ReportDetail.ReportMasterId}
<!-- alias: loc:ReportMaster, dom:ReportDetail_ReportMasterId -->

**What it is.** The drill-down report.

**Rules.** Required. Chosen from Report Master master data.

## ReportSetting {#ReportDetail.ReportSettingId}
<!-- alias: loc:ReportSetting, dom:ReportDetail_ReportSettingId -->

**What it is.** The column layout for this level.

**Rules.** Optional. Chosen from Report Setting master data.

## DrillDownLevel {#ReportDetail.DrillDownLevel}
<!-- alias: loc:DrillDownLevel, dom:ReportDetail_DrillDownLevel -->

**What it is.** The level number, from 1.

**Rules.** Required. Whole number.
