---
helpId: hs.safety-checklist.edit
lang: en
title: Safety Checklists — Create / Edit
entity: HCMv3.HealthAndSafetyManagement.SafetyChecklist
surfaces:
  - angular/src/app/admin/health-and-safety-management/safety-checklists/create-or-edit-safety-checklist-modal.component.html
audience: [admin]
permissions: [Pages.HealthAndSafetyManagement.SafetyChecklists.Create]
---

## Overview

A check to make on a safety area or asset. Leave the code blank to have the next number given.

## SafetyAreaAsset {#SafetyChecklist.SafetyAreaAssetId}
<!-- alias: loc:SafetyAreaAsset, dom:SafetyChecklist_SafetyAreaAssetId -->

**What it is.** The area or asset checked.

**Example.** Fire extinguishers

**Rules.** Required. Chosen from Safety Area Asset master data.

## IsActive {#SafetyChecklist.IsActive}
<!-- alias: loc:IsActive, dom:SafetyChecklist_IsActive -->

**What it is.** Whether the check is still in use.

**Rules.** Yes or no.

## Description {#SafetyChecklist.Description}
<!-- alias: loc:Description, dom:SafetyChecklist_Description -->

**What it is.** What to check.

**Rules.** Optional.
