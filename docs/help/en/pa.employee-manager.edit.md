---
helpId: pa.employee-manager.edit
lang: en
title: Employee Managers — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeManager
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/managers/create-or-edit-employee-manager-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeManagers.Create]
---

## Overview

Who the employee reports to, from a given date. Managers form a history: saving a new record closes the previous one on the day before the new start date.

The manager in force today is the one who sees this employee among their direct reports in self-service, receives the employee's requests at manager approval steps, and is copied on notifications set to go to the manager.

## Manager {#EmployeeManager.ManagerId}
<!-- alias: loc:Manager, dom:EmployeeManager_ManagerId -->

**What it is.** The employee's direct manager from this date.

**Example.** Fatima Al-Enezi (1002)

**Why it matters.** Drives self-service team visibility, manager approval steps (including higher levels, which follow each manager's own manager in turn), manager notifications, the organisation chart and manager reports. A record naming the employee as their own manager is ignored for all of these.

**Rules.** Required. Chosen from the employee list.
