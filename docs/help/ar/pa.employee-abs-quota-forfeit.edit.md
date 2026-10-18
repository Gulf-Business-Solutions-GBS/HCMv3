---
helpId: pa.employee-abs-quota-forfeit.edit
lang: ar
title: إسقاط حصص إجازات الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeAbsQuotaForfeit
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/abs-quota-forfeits/create-or-edit-employee-abs-quota-forfeit-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeAbsQuotaForfeits.Create]
---

## Overview

سجل يدوي لأيام إجازة فقدها الموظف من نوع إجازة.

لا يؤدي حفظه إلى تحديث سجل حصة إجازات الموظف، فلا يتغير الرصيد في الخدمة الذاتية. وتحتفظ احتسابات إسقاط الإجازات بسجلات الإسقاط الخاصة بها.

## EffectiveDate {#EmployeeAbsQuotaForfeit.EffectiveDate}
<!-- alias: loc:EffectiveDate, dom:EmployeeAbsQuotaForfeit_EffectiveDate -->

**ما هو.** تاريخ سريان الإسقاط.

**مثال.** 31 مارس 2026

**القواعد.** إلزامي. تاريخ.

## AbsenceTypeId {#EmployeeAbsQuotaForfeit.AbsenceTypeId}
<!-- alias: loc:AbsenceTypeId, dom:EmployeeAbsQuotaForfeit_AbsenceTypeId -->

**ما هو.** نوع الإجازة الذي تُسقط منه الأيام.

**مثال.** إجازة سنوية

**القواعد.** إلزامي. يُختار من قائمة أنواع الإجازات.

:::admin
تُدار من إدارة الرواتب ← أنواع الإجازات.
:::

## QuotaForfeited {#EmployeeAbsQuotaForfeit.QuotaForfeited}
<!-- alias: loc:QuotaForfeited, dom:EmployeeAbsQuotaForfeit_QuotaForfeited -->

**ما هو.** عدد الأيام المُسقطة.

**مثال.** 3

**القواعد.** اختياري. رقم.
