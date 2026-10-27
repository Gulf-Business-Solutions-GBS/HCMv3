---
helpId: pay.absence-payment.edit
lang: ar
title: مدفوعات الغياب — إنشاء / تحرير
entity: HCMv3.PayrollManagement.AbsencePayment
surfaces:
  - angular/src/app/admin/payroll-management/absence-payments/create-or-edit-absence-payment-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AbsencePayments.Create]
---

## Overview

نوع الأجر الذي يُصرف به نوع من أنواع الإجازات. لا تُقرأ حالياً: تُصرف الإجازة تحت نوع الأجر المحدد في نوع الإجازة نفسه، ولا شيء يقرأ هذه القائمة.

## AbsenceTypes {#AbsencePayment.AbsenceTypeId}
<!-- alias: loc:AbsenceTypes, dom:AbsencePayment_AbsenceTypeId -->

**ما هو.** نوع الإجازة.

**مثال.** الإجازة السنوية

**القواعد.** إلزامي. يُختار من قائمة أنواع الإجازات.

## WageTypes {#AbsencePayment.WageTypeId}
<!-- alias: loc:WageTypes, dom:AbsencePayment_WageTypeId -->

**ما هو.** نوع الأجر الذي يُصرف تحته.

**مثال.** أجر الإجازة السنوية

**القواعد.** إلزامي. يُختار من قائمة أنواع الأجور.
