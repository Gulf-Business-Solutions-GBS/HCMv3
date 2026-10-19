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

يؤدي حفظه إلى إضافة الأيام إلى رقم المُسقَط في حصة الإجازة السنوية للموظف التي تغطي تاريخ السريان، فينقص رصيد الإجازات؛ وعند تعديله تُرد أولاً الأيام المحفوظة سابقاً، وعند حذفه تُرد كلها. ولا تحرّك الحصةَ إلا أنواع الإجازة السنوية، وتكون الحصة دائماً حصة نوع الإجازة السنوية ذي الرمز 1. ولا يتحرك شيء لتاريخ يسبق أقدم تاريخ تقييم في إعدادات الوقت. ويُرفض الحفظ إذا زادت الأيام على الرصيد، أو إذا لم تكن للموظف حصة إجازة سنوية في ذلك التاريخ.

**عدم تحديث رصيد الإجازات** -- أشِّر عليه لحفظ السجل دون المساس بالحصة. وهو لا يُحفظ: إذ يظهر دائماً دون تأشير عند فتح النموذج، ويؤدي الحذف دائماً إلى رد الأيام. وتحتفظ احتسابات إسقاط الإجازات بسجلات الإسقاط الخاصة بها.

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
