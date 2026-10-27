---
helpId: pa.employee-dotted-line-manager.edit
lang: en
title: Employee Dotted Line Managers — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeDottedLineManager
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/dotted-line-managers/create-or-edit-employee-dotted-line-manager-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeDottedLineManagers.Create]
---

## Overview

A secondary, dotted-line manager for a period -- for example a project lead the employee also answers to.

It is recorded for reference only: dotted-line managers do not see the employee in self-service, do not approve their requests and do not receive their notifications.

## Manager {#EmployeeDottedLineManager.ManagerId}
<!-- alias: loc:Manager, dom:EmployeeDottedLineManager_ManagerId -->

**What it is.** The dotted-line manager for this period.

**Example.** Khaled Al-Rashidi (1044)

**Rules.** Required. Chosen from the employee list.
