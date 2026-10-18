---
helpId: pay.employee-sub-group-health-reimburse.edit
lang: ar
title: استردادات المصاريف الصحية للمجموعة الفرعية للموظفين — إنشاء / تحرير
entity: HCMv3.PayrollManagement.EmployeeSubGroupHealthReimburse
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/health-reimburses/create-or-edit-employee-sub-group-health-reimburse-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupHealthReimburses.Create]
---

## Overview

استحقاق الدرجة من استرداد المصاريف الصحية لكل شخص، بحسب نوع العائل -- سطر للموظف وسطر لكل نوع من العائلين.

ويجمع المبلغ المستحق للموظف سطر الموظف نفسه وأسطر عائليه، حتى الحد الأقصى لعدد العائلين في الدرجة. أما الحد السنوي وسنة التأمين فيُحدَّدان في الدرجة نفسها.

## DependantType {#EmployeeSubGroupHealthReimburse.DependantTypeId}
<!-- alias: loc:DependantType, dom:EmployeeSubGroupHealthReimburse_DependantTypeId -->

**ما هو.** لمن هذا السطر: الموظف، أو نوع من العائلين.

**مثال.** الزوج/الزوجة

**القواعد.** اختياري. يُختار من قائمة أنواع صفات العائل.

## Amount {#EmployeeSubGroupHealthReimburse.Amount}
<!-- alias: loc:Amount, dom:EmployeeSubGroupHealthReimburse_Amount -->

**ما هو.** المبلغ الذي يضيفه هذا الشخص إلى المبلغ المستحق.

**مثال.** `150`

**القواعد.** اختياري. رقم.

:::admin
استثناء المزايا للموظف يحل محل أسطر الموظف. واستثناء العائل يحل محل المجموع المعدود حتى تلك اللحظة كله بدلاً من أن يُضاف إليه -- وهذا سلوك النظام السابق وقد أُبقي عليه عمداً.
:::

## AgeLimit {#EmployeeSubGroupHealthReimburse.AgeLimit}
<!-- alias: loc:AgeLimit, dom:EmployeeSubGroupHealthReimburse_AgeLimit -->

**ما هو.** للعائل، أكبر عمر يُحتسب فيه.

**مثال.** `18`

**القواعد.** اختياري. رقم.

:::admin
حد العمر 0 يعني ألا يستحق أي عائل -- لا يعني "بلا حد". حدّد أكبر عمر يستحق.
:::
