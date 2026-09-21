---
helpId: pay.absence-violation-action-log.edit
lang: ar
title: سجلات تتبع إجراءات مخالفات الغياب — إنشاء / تحرير
entity: HCMv3.PayrollManagement.AbsenceViolationActionLog
surfaces:
  - angular/src/app/admin/payroll-management/absence-violation-action-logs/create-or-edit-absence-violation-action-log-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AbsenceViolationActionLogs.Create]
---

## Overview

خطوة واحدة اتُّخذت في سجل تصعيد: أي خطوة، وأي إجراء، ومتى.

لا تُكتب حالياً: لا توجد عملية تنشئ هذه السجلات، فتبقى القائمة كما تُدخل يدوياً.

## AbsenceViolationActionRecords {#AbsenceViolationActionLog.AbsenceViolationActionRecordId}
<!-- alias: loc:AbsenceViolationActionRecords, dom:AbsenceViolationActionLog_AbsenceViolationActionRecordId -->

**ما هو.** سجل التصعيد الذي تتبعه هذه الخطوة.

**القواعد.** إلزامي. يُختار من قائمة سجلات إجراءات مخالفات الغياب.

## Step {#AbsenceViolationActionLog.Step}
<!-- alias: loc:Step, dom:AbsenceViolationActionLog_Step -->

**ما هو.** خطوة القاعدة التي اتُّخذت.

**مثال.** `2`

**القواعد.** اختياري. رقم صحيح.

## Action {#AbsenceViolationActionLog.Action}
<!-- alias: loc:Action, dom:AbsenceViolationActionLog_Action -->

**ما هو.** الإجراء المتخذ.

**مثال.** إشعار

**القواعد.** إلزامي. إحدى القيم: إشعار، المخالفة، الإجازة.

## ActionDate {#AbsenceViolationActionLog.ActionDate}
<!-- alias: loc:ActionDate, dom:AbsenceViolationActionLog_ActionDate -->

**ما هو.** تاريخ اتخاذ الإجراء.

**مثال.** `2026-04-14`

**القواعد.** اختياري. تاريخ.

## ObjectOId {#AbsenceViolationActionLog.ObjectOId}
<!-- alias: loc:ObjectOId, dom:AbsenceViolationActionLog_ObjectOId -->

**ما هو.** الإشعار أو المخالفة أو الإجازة التي أنشأها الإجراء.

**القواعد.** اختياري. رقم صحيح.
