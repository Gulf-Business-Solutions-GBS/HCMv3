---
helpId: pay.absence-violation-action-record.edit
lang: ar
title: سجلات إجراءات مخالفات الغياب — إنشاء / تحرير
entity: HCMv3.PayrollManagement.AbsenceViolationActionRecord
surfaces:
  - angular/src/app/admin/payroll-management/absence-violation-action-records/create-or-edit-absence-violation-action-record-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AbsenceViolationActionRecords.Create]
---

## Overview

سجل لتصعيد مخالفة غياب أو حدث وقت، يبقى مفتوحاً حتى يُغلق.

لا تُكتب حالياً: لا توجد عملية تنشئ هذه السجلات، فتبقى القائمة كما تُدخل يدوياً.

## ActionLogType {#AbsenceViolationActionRecord.ActionLogType}
<!-- alias: loc:ActionLogType, dom:AbsenceViolationActionRecord_ActionLogType -->

**ما هو.** ما إذا كانت المخالفة ناشئة عن إجازة أو عن حدث وقت.

**مثال.** إجازة

**القواعد.** إلزامي. إحدى القيم: الإجازة، حدث الوقت.

## LogStatus {#AbsenceViolationActionRecord.LogStatus}
<!-- alias: loc:LogStatus, dom:AbsenceViolationActionRecord_LogStatus -->

**ما هو.** ما إذا كان التصعيد لا يزال مفتوحاً.

**مثال.** مفتوح

**القواعد.** إلزامي. إحدى القيم: مفتوح، مغلق.

## EntityObjectId {#AbsenceViolationActionRecord.EntityObjectId}
<!-- alias: loc:EntityObjectId, dom:AbsenceViolationActionRecord_EntityObjectId -->

**ما هو.** الإجازة أو حدث الوقت الذي يخصه السجل.

**القواعد.** اختياري. يُختار من قائمة.
