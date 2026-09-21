---
helpId: sys.general-settings.edit
lang: en
title: General settings
entity: HCMv3.SystemSettings.ManageGeneralSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-general-settings/create-or-edit-manage-general-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageGeneralSettings]
---

## Overview

General display settings.

Only the date and time formats are read at present -- for dates and times written into emails and generated messages. The rest has no effect.

There is one settings record per company; it cannot be deleted.

## DateEditMask {#ManageGeneralSettings.DateEditMask}
<!-- alias: loc:DateEditMask, dom:GeneralSettings_DateEditMask -->

**What it is.** The date format used in emails and generated messages.

**Example.** `dd/MM/yyyy`

**Why it matters.** Left blank, `dd/MM/yyyy` is used. Screens use the user's own language format, not this.

**Rules.** Optional.

## DateDisplayFormat {#ManageGeneralSettings.DateDisplayFormat}
<!-- alias: loc:DateDisplayFormat, dom:GeneralSettings_DateDisplayFormat -->

**What it is.** A date display format.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional.

## TimeEditMask {#ManageGeneralSettings.TimeEditMask}
<!-- alias: loc:TimeEditMask, dom:GeneralSettings_TimeEditMask -->

**What it is.** The time format used in emails and generated messages.

**Example.** `HH:mm`

**Why it matters.** Left blank, `HH:mm` is used.

**Rules.** Optional.

## TimeDisplayFormat {#ManageGeneralSettings.TimeDisplayFormat}
<!-- alias: loc:TimeDisplayFormat, dom:GeneralSettings_TimeDisplayFormat -->

**What it is.** A time display format.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional.

## StartupDashboard {#ManageGeneralSettings.StartupDashboard}
<!-- alias: loc:StartupDashboard, dom:GeneralSettings_StartupDashboard -->

**What it is.** The dashboard shown after sign-in.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional.

## AutoFilterColumnCondition {#ManageGeneralSettings.AutoFilterColumnCondition}
<!-- alias: loc:AutoFilterColumnCondition, dom:GeneralSettings_AutoFilterColumnCondition -->

**What it is.** How list filters match text.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Required. One of: Begins With, Contains, Equals.

## DisableAutoEmpcode {#ManageGeneralSettings.DisableAutoEmpcode}
<!-- alias: loc:DisableAutoEmpcode, dom:GeneralSettings_DisableAutoEmpcode -->

**What it is.** Whether employee codes are no longer assigned automatically.

**Why it matters.** Not read at present: a blank employee code is still given the next number when the employee is saved.

**Rules.** Yes or no.

## NoticePeriodDaysExcludingSubmissionDate {#ManageGeneralSettings.NoticePeriodDaysExcludingSubmissionDate}
<!-- alias: loc:NoticePeriodDaysExcludingSubmissionDate, dom:GeneralSettings_NoticePeriodDaysExcludingSubmissionDate -->

**What it is.** Whether the notice period excludes the day notice is given.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnableGridDataAccessModeServer {#ManageGeneralSettings.EnableGridDataAccessModeServer}
<!-- alias: loc:EnableGridDataAccessModeServer, dom:GeneralSettings_EnableGridDataAccessModeServer -->

**What it is.** Whether lists load their data page by page from the server.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## StartHangfire {#ManageGeneralSettings.StartHangfire}
<!-- alias: loc:StartHangfire, dom:GeneralSettings_StartHangfire -->

**What it is.** Whether the background job scheduler starts.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.
