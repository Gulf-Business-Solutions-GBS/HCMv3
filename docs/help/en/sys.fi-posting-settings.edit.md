---
helpId: sys.fi-posting-settings.edit
lang: en
title: FI posting settings
entity: HCMv3.SystemSettings.ManageFIPostingSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-fi-posting-settings/create-or-edit-manage-fi-posting-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageFIPostingSettings]
---

## Overview

How payroll is posted to finance.

There is one settings record per company; it cannot be deleted.

## SeparateProvisionPosting {#ManageFIPostingSettings.SeparateProvisionPosting}
<!-- alias: loc:SeparateProvisionPosting, dom:FIPostingSettings_SeparateProvisionPosting -->

**What it is.** Whether provisions are posted as a separate journal.

**Why it matters.** Ticked, posting the journal voucher splits it in two: salary lines in one journal and provision lines (leave, indemnity, ticket provisions) in another. Unticked, everything goes into one salary journal.

**Rules.** Yes or no.

## EnableAutoWageAccountEntry {#ManageFIPostingSettings.EnableAutoWageAccountEntry}
<!-- alias: loc:EnableAutoWageAccountEntry, dom:FIPostingSettings_EnableAutoWageAccountEntry -->

**What it is.** Whether wage accounts are created automatically.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## AllowDuplicateCodeInCostElement {#ManageFIPostingSettings.AllowDuplicateCodeInCostElement}
<!-- alias: loc:AllowDuplicateCodeInCostElement, dom:FIPostingSettings_AllowDuplicateCodeInCostElement -->

**What it is.** Whether two cost elements may share a code.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## FIPostingAfterPayrollExit {#ManageFIPostingSettings.FiPostingAfterPayrollExit}
<!-- alias: loc:FIPostingAfterPayrollExit, dom:FIPostingSettings_FIPostingAfterPayrollExit -->

**What it is.** Whether bank files may only be produced for closed payroll periods.

**Why it matters.** Despite its name, it gates bank files: ticked, a bank file whose end date is after the payroll area's last closed period is refused. Unticked, bank files can be produced for any dates.

**Rules.** Yes or no.
