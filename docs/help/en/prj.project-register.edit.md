---
helpId: prj.project-register.edit
lang: en
title: Project Registers — Create / Edit
entity: HCMv3.ProjectManagement.ProjectRegister
surfaces:
  - angular/src/app/admin/project-management/project-registers/create-or-edit-project-register-modal.component.html
audience: [admin]
permissions: [Pages.ProjectManagement.ProjectRegisters.Create]
---

## Overview

An employee's salary costs for one period, split by project. The Project Cost Engine builds it from the period's salary register; it runs only for a regular payroll whose control record is locked and current, and running it again replaces the figures. Written by the Project Cost Engine; there is normally no reason to add or change entries by hand.

## Employee {#ProjectRegister.EmployeeId}
<!-- alias: loc:Employee, dom:ProjectRegister_EmployeeId -->

**What it is.** The employee.

**Rules.** Required. Chosen from the employee list.

## Period {#ProjectRegister.Period}
<!-- alias: loc:Period, dom:ProjectRegister_Period -->

**What it is.** The payroll period.

**Rules.** Required. Date value.
