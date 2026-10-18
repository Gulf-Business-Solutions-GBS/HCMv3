---
helpId: pa.employee-tkt-adjustment.edit
lang: ar
title: تعديلات تذاكر الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeTktAdjustment
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/tkt-adjustments/create-or-edit-employee-tkt-adjustment-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeTktAdjustments.Create]
---

## Overview

تعديل يدوي على استحقاق الموظف من تذاكر الطيران.

يُسجَّل للرجوع إليه: فلا يؤدي حفظه إلى تحديث حصة تذاكر الموظف، ولا تقرؤه الرواتب ولا محرك الوقت حالياً.

## EffectiveDate {#EmployeeTktAdjustment.EffectiveDate}
<!-- alias: loc:EffectiveDate, dom:EmployeeTktAdjustment_EffectiveDate -->

**ما هو.** تاريخ سريان التعديل.

**مثال.** 30 يونيو 2026

**القواعد.** إلزامي. تاريخ.

## TicketTypeId {#EmployeeTktAdjustment.TicketTypeId}
<!-- alias: loc:TicketTypeId, dom:EmployeeTktAdjustment_TicketTypeId -->

**ما هو.** نوع التذكرة المعدَّل.

**مثال.** الموظف

**القواعد.** إلزامي. يُختار من قائمة أنواع التذاكر.

## AdjustmentType {#EmployeeTktAdjustment.AdjustmentType}
<!-- alias: loc:AdjustmentType, dom:EmployeeTktAdjustment_AdjustmentType -->

**ما هو.** نوع التعديل.

**مثال.** صرف نقدي

**القواعد.** إلزامي. إحدى القيم: الصرف النقدي، تعديل، كسري، بلا أثر على الحصة.

## TicketPaymentType {#EmployeeTktAdjustment.TicketPaymentType}
<!-- alias: loc:TicketPaymentType, dom:EmployeeTktAdjustment_TicketPaymentType -->

**ما هو.** متى يُصرف النقد.

**مثال.** مع الرواتب

**القواعد.** إلزامي. إحدى القيم: الدفع مع الرواتب، الدفع مقدماً.

## QuotaAccrued {#EmployeeTktAdjustment.QuotaAccrued}
<!-- alias: loc:QuotaAccrued, dom:EmployeeTktAdjustment_QuotaAccrued -->

**ما هو.** التذاكر المتراكمة بالتعديل.

**مثال.** 0

**القواعد.** اختياري. رقم.

## QuotaAdjusted {#EmployeeTktAdjustment.QuotaAdjusted}
<!-- alias: loc:QuotaAdjusted, dom:EmployeeTktAdjustment_QuotaAdjusted -->

**ما هو.** التذاكر المعدَّلة.

**مثال.** 1

**القواعد.** اختياري. رقم.
