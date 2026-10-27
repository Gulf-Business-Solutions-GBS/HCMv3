---
helpId: sys.project-costing-settings.edit
lang: en
title: Project costing settings
entity: HCMv3.SystemSettings.ManageProjectCostingSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-project-costing-settings/create-or-edit-manage-project-costing-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageProjectCostingSettings]
---

## Overview

Settings for project costing.

There is one settings record per company; it cannot be deleted.

## CalculateNetPayProjectWise {#ManageProjectCostingSettings.CalculateNetPayProjectWise}
<!-- alias: loc:CalculateNetPayProjectWise, dom:ProjectCostingSettings_CalculateNetPayProjectWise -->

**What it is.** Whether net pay is spread across the employee's projects.

**Why it matters.** Ticked, the project cost engine distributes net pay to projects as well as the individual wagetypes. Unticked, net pay is not costed to projects.

**Rules.** Yes or no.
