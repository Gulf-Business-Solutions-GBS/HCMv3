---
helpId: sec.execution-log.edit
lang: en
title: Execution Logs — Create / Edit
entity: HCMv3.SystemSecurity.ExecutionLog
surfaces:
  - angular/src/app/admin/system-security/execution-logs/create-or-edit-execution-log-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.ExecutionLogs.Create]
---

## Overview

One run of an engine, import, report or print: who ran it, when, with what parameters, and how it ended. The execution log report lists these. Written by the system as it runs; there is normally no reason to add or change entries by hand.

## UserName {#ExecutionLog.UserName}
<!-- alias: loc:UserName, dom:ExecutionLog_UserName -->

**What it is.** Who ran it.

**Rules.** Optional. Maximum 256 characters.

## ActionDateTime {#ExecutionLog.ActionDateTime}
<!-- alias: loc:ActionDateTime, dom:ExecutionLog_ActionDateTime -->

**What it is.** When it ran.

**Rules.** Optional. Date value.

## Action {#ExecutionLog.Action}
<!-- alias: loc:Action, dom:ExecutionLog_Action -->

**What it is.** What was done.

**Example.** Run

**Rules.** Required. One of: Run, Generate, View, Export, Print.

## Module {#ExecutionLog.Module}
<!-- alias: loc:Module, dom:ExecutionLog_Module -->

**What it is.** Which part of the system.

**Example.** Payroll Engine

**Rules.** Required. One of: Time Engine, Payroll Engine, Bank File, Grid Report, Print Report, Cost Engine, Project Engine.

## ScreenName {#ExecutionLog.ScreenName}
<!-- alias: loc:ScreenName, dom:ExecutionLog_ScreenName -->

**What it is.** The screen it was run from.

**Rules.** Optional. Maximum 256 characters.

## ScreenCaption {#ExecutionLog.ScreenCaption}
<!-- alias: loc:ScreenCaption, dom:ExecutionLog_ScreenCaption -->

**What it is.** The screen's title.

**Rules.** Optional. Maximum 256 characters.

## ExecutionStatus {#ExecutionLog.ExecutionStatus}
<!-- alias: loc:ExecutionStatus, dom:ExecutionLog_ExecutionStatus -->

**What it is.** How it ended.

**Example.** Success

**Rules.** Required. One of: In Process, Success, Failed, Partial, Aborted.

## TotalRecords {#ExecutionLog.TotalRecords}
<!-- alias: loc:TotalRecords, dom:ExecutionLog_TotalRecords -->

**What it is.** Records processed.

**Example.** `240`

**Rules.** Optional. Whole number.

## Successfull {#ExecutionLog.Successfull}
<!-- alias: loc:Successfull, dom:ExecutionLog_Successfull -->

**What it is.** Records that succeeded.

**Example.** `238`

**Rules.** Optional. Whole number.

## Failed {#ExecutionLog.Failed}
<!-- alias: loc:Failed, dom:ExecutionLog_Failed -->

**What it is.** Records that failed.

**Example.** `2`

**Rules.** Optional. Whole number.

## Parameters {#ExecutionLog.Parameters}
<!-- alias: loc:Parameters, dom:ExecutionLog_Parameters -->

**What it is.** The parameters it ran with.

**Rules.** Optional.
