---
helpId: pa.employee-basic-pay-detail.edit
lang: ar
title: تفاصيل الراتب الأساسي للموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeBasicPayDetail
surfaces:
  - angular/src/app/admin/personnel-administration/employee-basic-pay-details/create-or-edit-employee-basic-pay-detail-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeBasicPayDetails.Create]
---

## Overview

نوع أجر ومبلغه ضمن سجل الأجر الأساسي -- كالراتب الأساسي أو بدل السكن أو بدل المواصلات. تحتسب الرواتب كل نوع أجر من البند التفصيلي في سجل الأجر الأساسي الساري في تاريخ الدفع.

لا يجوز أن يتكرر نوع الأجر في سجل الأجر الأساسي الواحد.

## EmployeeBasicPayId {#EmployeeBasicPayDetail.EmployeeBasicPayId}
<!-- alias: loc:EmployeeBasicPayId, dom:EmployeeBasicPayDetail_EmployeeBasicPayId -->

**ما هو.** سجل الأجر الأساسي الذي ينتمي إليه هذا البند.

**القواعد.** إلزامي. يُختار من قائمة.

## WageTypeId {#EmployeeBasicPayDetail.WageTypeId}
<!-- alias: loc:WageTypeId, dom:EmployeeBasicPayDetail_WageTypeId -->

**ما هو.** عنصر الأجر الذي يدفعه هذا البند.

**مثال.** الراتب الأساسي

**لماذا يهم.** لا يتكرر نوع الأجر في سجل الأجر الأساسي الواحد.

**القواعد.** إلزامي. يُختار من قائمة أنواع الأجور.

:::admin
يُدار من إدارة الرواتب ← أنواع الأجور.
:::

## Amount {#EmployeeBasicPayDetail.Amount}
<!-- alias: loc:Amount, dom:EmployeeBasicPayDetail_Amount -->

**ما هو.** معدل نوع الأجر هذا، وفق دورية الأجر في سجل الأجر الأساسي.

**مثال.** 850.000

**لماذا يهم.** تستخدمه الرواتب معدلاً شهرياً أو يومياً أو بالساعة لنوع الأجر هذا، ويُنقل إلى قيود التكلفة والقيود المحاسبية.

**القواعد.** اختياري. رقم.

## CurrencyId {#EmployeeBasicPayDetail.CurrencyId}
<!-- alias: loc:CurrencyId, dom:EmployeeBasicPayDetail_CurrencyId -->

**ما هو.** عملة المبلغ.

**مثال.** KWD

**لماذا يهم.** يقرؤها ملف القيود المحاسبية لترحيل أجر الموظف بهذه العملة.

**القواعد.** إلزامي. يُختار من قائمة العملات.

## DependantUnit {#EmployeeBasicPayDetail.DependantUnit}
<!-- alias: loc:DependantUnit, dom:EmployeeBasicPayDetail_DependantUnit -->

**ما هو.** هل يعتمد هذا البند على نوع أجر آخر، وكيف.

**مثال.** نسبة مئوية

**لماذا يهم.** لا يُستخدم إلا عند اختيار نوع أجر معتمَد عليه.

**القواعد.** إلزامي. إحدى القيم: لا شيء، النسبة المئوية.

## DependantNumber {#EmployeeBasicPayDetail.DependantNumber}
<!-- alias: loc:DependantNumber, dom:EmployeeBasicPayDetail_DependantNumber -->

**ما هو.** النسبة المئوية من مبلغ نوع الأجر المعتمَد عليه.

**مثال.** 25

**القواعد.** اختياري. رقم.

## DependantWageTypeId {#EmployeeBasicPayDetail.DependantWageTypeId}
<!-- alias: loc:DependantWageTypeId, dom:EmployeeBasicPayDetail_DependantWageTypeId -->

**ما هو.** نوع أجر آخر يُحتسب منه هذا البند.

**مثال.** الراتب الأساسي

**القواعد.** اختياري. يُختار من قائمة أنواع الأجور.

:::admin
عند تحديد هذا الحقل، تحتسب الرواتب النسبة من مبلغ نوع الأجر ذاك، لكنها تحفظ الناتج في عدد بند الرواتب لا في مبلغه -- وهو سلوك منقول من النظام السابق. لذلك لا يدفع البند حالياً النسبة المحتسبة.
:::
