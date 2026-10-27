---
helpId: pa.employee-work-experience.edit
lang: en
title: Employee Work Experiences — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeWorkExperience
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/work-experience/create-or-edit-employee-work-experience-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeWorkExperiences.Create]
---

## Overview

A previous job the employee held before joining. Periods for the same employee cannot overlap.

It is recorded for reference; no calculation or report reads it at present.

## Company {#EmployeeWorkExperience.Company}
<!-- alias: loc:Company, dom:EmployeeWorkExperience_Company -->

**What it is.** The previous employer.

**Example.** Gulf Trading Co.

**Rules.** Required. Maximum 256 characters.

## Position {#EmployeeWorkExperience.Position}
<!-- alias: loc:Position, dom:EmployeeWorkExperience_Position -->

**What it is.** The job title held there, as free text.

**Example.** Accountant

**Rules.** Optional. Maximum 256 characters.

## Location {#EmployeeWorkExperience.Location}
<!-- alias: loc:Location, dom:EmployeeWorkExperience_Location -->

**What it is.** Where the job was.

**Example.** Kuwait City

**Rules.** Optional. Maximum 256 characters.

## ReasonForLeaving {#EmployeeWorkExperience.ReasonForLeaving}
<!-- alias: loc:ReasonForLeaving, dom:EmployeeWorkExperience_ReasonForLeaving -->

**What it is.** Why the employee left.

**Example.** Career progression

**Rules.** Optional. Maximum 1000 characters.
