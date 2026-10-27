---
helpId: br.business-rule.edit
lang: en
title: Business Rules — Create / Edit
entity: HCMv3.BusinessRules.BusinessRule
surfaces:
  - angular/src/app/admin/business-rules/create-or-edit-business-rule-modal.component.html
audience: [admin]
permissions: [Pages.BusinessRules]
---

## Overview

A business rule: conditions and actions that run automatically when something happens to a record, written either visually (if / then / else blocks) or as a formula. A rule is built on a base object -- Employee, Employee Absence, or Workflow Routing -- which decides the fields it can read and set. On Employee and Employee Absence, rules run before a record is saved (setting values first, then validating) and before it is deleted; a rule can raise an error that stops the save. On Workflow Routing, a rule can name the approval chain a request uses, ahead of workflow groups. A rule runs only while it is active and today falls between its effective dates, and only through an assignment that is active for the event; assignments on the same event run in execution order. Use Run test to try a rule before saving it.

## Active {#BusinessRule.IsActive}
<!-- alias: loc:Active, dom:BusinessRule_IsActive -->

**What it is.** Whether the rule runs.

**Why it matters.** An inactive rule is skipped by every assignment.

**Rules.** Yes or no.
