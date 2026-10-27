---
helpId: sys.org-settings.edit
lang: en
title: Organization settings
entity: HCMv3.SystemSettings.ManageOrgSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-org-settings/create-or-edit-manage-org-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageOrgSettings]
---

## Overview

Which organisation-management features are switched on.

None of these settings is read at present: the features they control were not carried over from the legacy system, so changing them has no effect.

There is one settings record per company; it cannot be deleted.

## EnablePostingGroup {#ManageOrgSettings.EnablePostingGroup}
<!-- alias: loc:EnablePostingGroup, dom:OrgSettings_EnablePostingGroup -->

**What it is.** Whether posting groups are used.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnableBudgetingGroup {#ManageOrgSettings.EnableBudgetingGroup}
<!-- alias: loc:EnableBudgetingGroup, dom:OrgSettings_EnableBudgetingGroup -->

**What it is.** Whether budgeting groups are used.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnablePosition {#ManageOrgSettings.EnablePosition}
<!-- alias: loc:EnablePosition, dom:OrgSettings_EnablePosition -->

**What it is.** Whether positions are used.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnableOrgChart3Levels {#ManageOrgSettings.EnableOrgChart3Levels}
<!-- alias: loc:EnableOrgChart3Levels, dom:OrgSettings_EnableOrgChart3Levels -->

**What it is.** Whether the organisation chart shows three levels.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.
