---
helpId: sec.bulk-upload-log.edit
lang: en
title: Bulk Upload Logs — Create / Edit
entity: HCMv3.SystemSecurity.BulkUploadLog
surfaces:
  - angular/src/app/admin/system-security/bulk-upload-logs/create-or-edit-bulk-upload-log-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.BulkUploadLogs.Create]
---

## Overview

Entries written by a bulk upload, such as the profile-photo import. The bulk import screen shows the latest run's progress from them. Written by the system as it runs; there is normally no reason to add or change entries by hand.

## Employee {#BulkUploadLog.EmployeeId}
<!-- alias: loc:Employee, dom:BulkUploadLog_EmployeeId -->

**What it is.** The employee the entry is about.

**Rules.** Required. Chosen from the employee list.

## LogType {#BulkUploadLog.LogType}
<!-- alias: loc:LogType, dom:BulkUploadLog_LogType -->

**What it is.** What was uploaded.

**Example.** Profile Photo

**Rules.** Required. One of: Profile Photo.

## RunTimeStamp {#BulkUploadLog.RunTimeStamp}
<!-- alias: loc:RunTimeStamp, dom:BulkUploadLog_RunTimeStamp -->

**What it is.** When the entry was written.

**Rules.** Optional. Date value.

## InPeriod {#BulkUploadLog.InPeriod}
<!-- alias: loc:InPeriod, dom:BulkUploadLog_InPeriod -->

**What it is.** The period of the run.

**Rules.** Optional. Date value.

## Status {#BulkUploadLog.Status}
<!-- alias: loc:Status, dom:BulkUploadLog_Status -->

**What it is.** The outcome: 0 is success.

**Example.** `0`

**Rules.** Optional. Whole number.

## NoOfEmployee {#BulkUploadLog.NoOfEmployee}
<!-- alias: loc:NoOfEmployee, dom:BulkUploadLog_NoOfEmployee -->

**What it is.** How many records the run was processing.

**Rules.** Optional. Whole number.

## RecordNo {#BulkUploadLog.RecordNo}
<!-- alias: loc:RecordNo, dom:BulkUploadLog_RecordNo -->

**What it is.** The record's position in the run.

**Rules.** Optional. Whole number.

## StopFlag {#BulkUploadLog.StopFlag}
<!-- alias: loc:StopFlag, dom:BulkUploadLog_StopFlag -->

**What it is.** Whether the run was asked to stop.

**Why it matters.** A running import stops when it finds an entry flagged.

**Rules.** Yes or no.

## Message {#BulkUploadLog.Message}
<!-- alias: loc:Message, dom:BulkUploadLog_Message -->

**What it is.** What happened.

**Rules.** Optional.
