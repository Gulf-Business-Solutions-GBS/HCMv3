---
helpId: sys.security-settings.edit
lang: en
title: Security settings
entity: HCMv3.SystemSettings.ManageSecuritySettings
surfaces:
  - angular/src/app/admin/system-settings/manage-security-settings/create-or-edit-manage-security-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageSecuritySettings]
---

## Overview

Security and audit settings.

There is one settings record per company; it cannot be deleted.

## EnableRowLevelSecurity {#ManageSecuritySettings.EnableRowLevelSecurity}
<!-- alias: loc:EnableRowLevelSecurity, dom:SecuritySettings_EnableRowLevelSecurity -->

**What it is.** Whether row-level security is applied.

**Why it matters.** Ticked, the places that apply row-level security limit users to the employees their security assignments allow, and scheduled report recipients are limited the same way. Unticked, no row-level restriction applies.

**Rules.** Yes or no.

## EnableReportExportPwd {#ManageSecuritySettings.EnableReportExportPwd}
<!-- alias: loc:EnableReportExportPwd, dom:SecuritySettings_EnableReportExportPwd -->

**What it is.** Whether exported reports are password protected.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnableAuditLogInTimeEngine {#ManageSecuritySettings.EnableAuditLogInTimeEngine}
<!-- alias: loc:EnableAuditLogInTimeEngine, dom:SecuritySettings_EnableAuditLogInTimeEngine -->

**What it is.** Whether time engine changes are written to the audit log.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnableAuditLogInPayrollEngine {#ManageSecuritySettings.EnableAuditLogInPayrollEngine}
<!-- alias: loc:EnableAuditLogInPayrollEngine, dom:SecuritySettings_EnableAuditLogInPayrollEngine -->

**What it is.** Whether payroll engine changes are written to the audit log.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnableAuditLogInProjectCostEngine {#ManageSecuritySettings.EnableAuditLogInProjectCostEngine}
<!-- alias: loc:EnableAuditLogInProjectCostEngine, dom:SecuritySettings_EnableAuditLogInProjectCostEngine -->

**What it is.** Whether project cost engine changes are written to the audit log.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnableAuditLogInCostEngine {#ManageSecuritySettings.EnableAuditLogInCostEngine}
<!-- alias: loc:EnableAuditLogInCostEngine, dom:SecuritySettings_EnableAuditLogInCostEngine -->

**What it is.** Whether cost engine changes are written to the audit log.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnableAuditLogInGridReport {#ManageSecuritySettings.EnableAuditLogInGridReport}
<!-- alias: loc:EnableAuditLogInGridReport, dom:SecuritySettings_EnableAuditLogInGridReport -->

**What it is.** Whether running a report is logged.

**Why it matters.** Ticked, each report run writes an execution log entry. Unticked, report runs are not logged.

**Rules.** Yes or no.

## EnableAuditLogInPrintReport {#ManageSecuritySettings.EnableAuditLogInPrintReport}
<!-- alias: loc:EnableAuditLogInPrintReport, dom:SecuritySettings_EnableAuditLogInPrintReport -->

**What it is.** Whether printing a report is logged.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.
