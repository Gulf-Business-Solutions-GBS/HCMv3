---
helpId: cd.cdform.edit
lang: en
title: CD Forms — Create / Edit
entity: HCMv3.CompetencyDevelopment.CDForm
surfaces:
  - angular/src/app/admin/competency-development/cd-forms/create-or-edit-cd-form-modal.component.html
audience: [admin]
permissions: [Pages.CompetencyDevelopment.CDForms.Create]
---

## Overview

An employee's competency development form for one period, holding their plan, mid-year review and annual review. An employee can have one form per period.

## CDPeriod {#CDForm.CDPeriodId}
<!-- alias: loc:CDPeriod, dom:CDForm_CDPeriodId -->

**What it is.** The review period.

**Rules.** Required. Chosen from CD Period master data.

## Employee {#CDForm.EmployeeId}
<!-- alias: loc:Employee, dom:CDForm_EmployeeId -->

**What it is.** The employee.

**Rules.** Required. Chosen from the employee list.

## ModeratedScore {#CDForm.ModeratedScore}
<!-- alias: loc:ModeratedScore, dom:CDForm_ModeratedScore -->

**What it is.** The final score after moderation, entered by hand.

**Example.** 3.5

**Rules.** Optional. Number.

## EmployeeSignedOff {#CDForm.EmployeeSignedOff}
<!-- alias: loc:EmployeeSignedOff, dom:CDForm_EmployeeSignedOff -->

**What it is.** Whether the employee has agreed the form.

**Why it matters.** A record only: the form can still be changed.

**Rules.** Yes or no.

## ManagerSignedOff {#CDForm.ManagerSignedOff}
<!-- alias: loc:ManagerSignedOff, dom:CDForm_ManagerSignedOff -->

**What it is.** Whether the manager has agreed the form.

**Why it matters.** A record only: the form can still be changed.

**Rules.** Yes or no.
