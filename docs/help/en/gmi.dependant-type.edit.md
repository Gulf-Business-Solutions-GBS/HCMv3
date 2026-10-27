---
helpId: gmi.dependant-type.edit
lang: en
title: Dependant Types — Create / Edit
entity: HCMv3.GeneralMasterInfo.DependantType
surfaces:
  - angular/src/app/admin/general-master-info/dependant-types/create-or-edit-dependant-type-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.DependantTypes.Create]
---

## Overview

The kinds of dependant an employee can have -- spouse, son, daughter and so on. Every dependant recorded on an employee has one of these types, and health reimbursement limits are set per dependant type.

## Gender {#DependantType.Gender}
<!-- alias: loc:Gender, dom:DependantType_Gender -->

**What it is.** The gender this dependant type implies -- a son is male, a daughter female.

**Why it matters.** Recorded for information; nothing checks a dependant's gender against it.

**Rules.** Required. One of: Male, Female.

## IsEmployee {#DependantType.IsEmployee}
<!-- alias: loc:IsEmployee, dom:DependantType_IsEmployee -->

**What it is.** Marks the type that stands for the employee themself rather than a relative.

**Why it matters.** Health reimbursement uses it to tell the employee's own limit apart from their dependants'. Only one dependant type can have it ticked.

**Rules.** Yes or no.
