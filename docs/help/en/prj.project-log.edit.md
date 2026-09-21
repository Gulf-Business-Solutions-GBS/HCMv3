---
helpId: prj.project-log.edit
lang: en
title: Project Logs — Create / Edit
entity: HCMv3.ProjectManagement.ProjectLog
surfaces:
  - angular/src/app/admin/project-management/project-logs/create-or-edit-project-log-modal.component.html
audience: [admin]
permissions: [Pages.ProjectManagement.ProjectLogs.Create]
---

## Overview

A line in the Project Cost Engine's run log: one per employee, plus a line for a run that failed before starting. The engine screen reads it to show progress. Written by the Project Cost Engine; there is normally no reason to add or change entries by hand.

## Employee {#ProjectLog.EmployeeId}
<!-- alias: loc:Employee, dom:ProjectLog_EmployeeId -->

**What it is.** The employee processed, if any.

**Rules.** Required. Chosen from the employee list.

## RunTimeStamp {#ProjectLog.RunTimeStamp}
<!-- alias: loc:RunTimeStamp, dom:ProjectLog_RunTimeStamp -->

**What it is.** When the line was written.

**Rules.** Optional. Date value.

## Message {#ProjectLog.Message}
<!-- alias: loc:Message, dom:ProjectLog_Message -->

**What it is.** The outcome, or the error.

**Rules.** Optional.

## Status {#ProjectLog.Status}
<!-- alias: loc:Status, dom:ProjectLog_Status -->

**What it is.** 0 when the employee was processed successfully; any other value is a failure.

**Rules.** Required. Whole number.

## InPeriod {#ProjectLog.InPeriod}
<!-- alias: loc:InPeriod, dom:ProjectLog_InPeriod -->

**What it is.** The payroll period run.

**Rules.** Optional. Date value.

## NoOfEmployee {#ProjectLog.NoOfEmployee}
<!-- alias: loc:NoOfEmployee, dom:ProjectLog_NoOfEmployee -->

**What it is.** How many employees the run covers.

**Rules.** Required. Whole number.

## StopFlag {#ProjectLog.StopFlag}
<!-- alias: loc:StopFlag, dom:ProjectLog_StopFlag -->

**What it is.** A request to stop the run.

**Why it matters.** While any line has it ticked, the engine stops before the next employee.

**Rules.** Yes or no.
