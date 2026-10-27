---
helpId: rpt.print-report-permission.edit
lang: en
title: Print Report Permissions — Create / Edit
entity: HCMv3.ReportManagement.PrintReportPermission
surfaces:
  - angular/src/app/admin/report-management/print-report-permissions/create-or-edit-print-report-permission-modal.component.html
audience: [admin]
permissions: [Pages.ReportManagement.PrintReportPermissions.Create]
---

## Overview

Which print reports the users in one role may run. A user sees the reports granted to any of their roles. A user with no grants sees none, but company administrators and host users always see every active report. Each role can have one entry.

## Role {#PrintReportPermission.RoleId}
<!-- alias: loc:Role, dom:PrintReportPermission_RoleId -->

**What it is.** The role being granted reports.

**Example.** Payroll

**Rules.** Required. Chosen from Role master data.
