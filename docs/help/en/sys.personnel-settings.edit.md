---
helpId: sys.personnel-settings.edit
lang: en
title: Personnel settings
entity: HCMv3.SystemSettings.ManagePersonnelSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-personnel-settings/create-or-edit-manage-personnel-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManagePersonnelSettings]
---

## Overview

Settings for personnel administration.

There is one settings record per company; it cannot be deleted.

## ProbationDaysCalculation {#ManagePersonnelSettings.ProbationDaysCalculation}
<!-- alias: loc:ProbationDaysCalculation, dom:PersonnelSettings_ProbationDaysCalculation -->

**What it is.** Whether probation days are counted in calendar days or working days.

**Example.** Calendar Days

**Why it matters.** Only *Calendar Days* is supported: probation end is always the joining date plus the probation days. With *Working Days*, the probation recalculation tool refuses to run.

**Rules.** Required. One of: Calendar Days, Working Days.

## MaxPhotoSize {#ManagePersonnelSettings.MaxPhotoSize}
<!-- alias: loc:MaxPhotoSize, dom:PersonnelSettings_MaxPhotoSize -->

**What it is.** The largest employee photo accepted by the bulk photo import, in KB.

**Example.** `500`

**Why it matters.** 0 uses the import's own default. Photos chosen on the employee form are not limited by it.

**Rules.** Optional. Whole number.

## EnableSponsorGovtJobTitleQuota {#ManagePersonnelSettings.EnableSponsorGovtJobTitleQuota}
<!-- alias: loc:EnableSponsorGovtJobTitleQuota, dom:PersonnelSettings_EnableSponsorGovtJobTitleQuota -->

**What it is.** Whether sponsor job-title quotas are calculated.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## AutoWageAssignOnHire {#ManagePersonnelSettings.AutoWageAssignOnHire}
<!-- alias: loc:AutoWageAssignOnHire, dom:PersonnelSettings_AutoWageAssignOnHire -->

**What it is.** Whether wagetypes are assigned automatically when an employee is hired.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.
