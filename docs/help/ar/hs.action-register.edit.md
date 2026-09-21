---
helpId: hs.action-register.edit
lang: ar
title: سجلات الإجراءات — إنشاء / تحرير
entity: HCMv3.HealthAndSafetyManagement.ActionRegister
surfaces:
  - angular/src/app/admin/health-and-safety-management/action-registers/create-or-edit-action-register-modal.component.html
audience: [admin]
permissions: [Pages.HealthAndSafetyManagement.ActionRegisters.Create]
---

## Overview

إجراء تصحيحي أو وقائي. وكما في النظام القديم، يجب اختيار حادث وخطر معاً.

## ActionDate {#ActionRegister.ActionDate}
<!-- alias: loc:ActionDate, dom:ActionRegister_ActionDate -->

**ما هو.** متى أُنشئ الإجراء.

**القواعد.** إلزامي. تاريخ.

## Status {#ActionRegister.StatusId}
<!-- alias: loc:Status, dom:ActionRegister_StatusId -->

**ما هو.** حالة الإجراء.

**مثال.** قيد التنفيذ

**القواعد.** اختياري. يُختار من قائمة حالات الإجراءات.

## Incident {#ActionRegister.IncidentId}
<!-- alias: loc:Incident, dom:ActionRegister_IncidentId -->

**ما هو.** الحادث الذي يخصه هذا.

**القواعد.** إلزامي. يُختار من قائمة سجلات الحوادث.

## Risk {#ActionRegister.RiskId}
<!-- alias: loc:Risk, dom:ActionRegister_RiskId -->

**ما هو.** الخطر الذي يخصه هذا.

**القواعد.** إلزامي. يُختار من قائمة سجلات المخاطر.

## AssignedTo {#ActionRegister.AssignedToId}
<!-- alias: loc:AssignedTo, dom:ActionRegister_AssignedToId -->

**ما هو.** من يجب أن ينفذه.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## Description {#ActionRegister.Description}
<!-- alias: loc:Description, dom:ActionRegister_Description -->

**ما هو.** ما يجب القيام به.

**القواعد.** اختياري.
