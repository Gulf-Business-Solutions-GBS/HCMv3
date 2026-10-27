---
helpId: rpt.print-report-permission-detail.edit
lang: en
title: Print Report Permission Details — Create / Edit
entity: HCMv3.ReportManagement.PrintReportPermissionDetail
surfaces:
  - angular/src/app/admin/report-management/print-report-permission-details/create-or-edit-print-report-permission-detail-modal.component.html
audience: [admin]
permissions: [Pages.ReportManagement.PrintReportPermissionDetails.Create]
---

## Overview

One report granted to a role. The same report can be added once per role.

## PrintReportPermission {#PrintReportPermissionDetail.PrintReportPermissionId}
<!-- alias: loc:PrintReportPermission, dom:PrintReportPermissionDetail_PrintReportPermissionId -->

**What it is.** The role's report grant this belongs to.

**Rules.** Required. Chosen from Print Report Permission master data.

## PrintReport {#PrintReportPermissionDetail.PrintReportId}
<!-- alias: loc:PrintReport, dom:PrintReportPermissionDetail_PrintReportId -->

**What it is.** The report granted.

**Rules.** Required. Chosen from Print Report master data.
