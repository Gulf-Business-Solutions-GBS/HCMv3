---
helpId: pa.employee-wage-account.edit
lang: ar
title: حسابات أجور الموظف — إنشاء / تحرير
entity: HCMv3.PayrollManagement.EmployeeWageAccount
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/wage-accounts/create-or-edit-employee-wage-account-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeWageAccounts.Create]
---

## Overview

الحساب في دفتر الأستاذ الذي يُرحَّل إليه أحد أنواع أجور الموظف اعتباراً من تاريخ معين. يقرأ محرك التكلفة وملفات القيود المحاسبية هذه السجلات لترحيل أجر الموظف.

تشكّل سجلات الموظف ونوع الأجر نفسه تاريخاً متتابعاً: فعند حفظ سجل جديد يُغلق السابق في اليوم السابق لتاريخ البداية الجديد. ويمكن توزيع نوع الأجر على عدة حسابات تبدأ في التاريخ نفسه، بشرط ألا يزيد مجموع النسب على 100.

## WageTypeId {#EmployeeWageAccount.WageTypeId}
<!-- alias: loc:WageTypeId, dom:EmployeeWageAccount_WageTypeId -->

**ما هو.** نوع الأجر الذي يخص هذا الربط.

**مثال.** بدل السكن

**القواعد.** إلزامي. يُختار من قائمة أنواع الأجور.

:::admin
يُدار من إدارة الرواتب ← أنواع الأجور. ولا يُرحَّل إلا نوع الأجر المشمول بالترحيل المالي.
:::

## CostPercentage {#EmployeeWageAccount.CostPercentage}
<!-- alias: loc:CostPercentage, dom:EmployeeWageAccount_CostPercentage -->

**ما هو.** حصة مبلغ نوع الأجر التي تُرحَّل إلى هذا الحساب.

**مثال.** 100

**لماذا يهم.** يرحّل محرك التكلفة هذه النسبة من إجمالي نوع الأجر. ولا يجوز أن يزيد مجموع سجلات الموظف ونوع الأجر وتاريخ البداية نفسه على 100.

**القواعد.** اختياري. رقم.

## AccountInfoId {#EmployeeWageAccount.AccountInfoId}
<!-- alias: loc:AccountInfoId, dom:EmployeeWageAccount_AccountInfoId -->

**ما هو.** حساب دفتر الأستاذ الذي يُرحَّل إليه المبلغ.

**مثال.** 510100 — الرواتب والأجور

**القواعد.** إلزامي. يُختار من قائمة بيانات الحسابات.

## OffsetAccountInfoId {#EmployeeWageAccount.OffsetAccountInfoId}
<!-- alias: loc:OffsetAccountInfoId, dom:EmployeeWageAccount_OffsetAccountInfoId -->

**ما هو.** الحساب المقابل للقيد المحاسبي.

**مثال.** 210500 — رواتب مستحقة

**لماذا يهم.** عند تحديده، يرحّل ملف القيود الطرف الآخر من القيد إلى هذا الحساب.

**القواعد.** اختياري. يُختار من قائمة بيانات الحسابات.

## MergeOffsetAccount {#EmployeeWageAccount.MergeOffsetAccount}
<!-- alias: loc:MergeOffsetAccount, dom:EmployeeWageAccount_MergeOffsetAccount -->

**ما هو.** هل تُدمج أسطر الحساب المقابل للحساب نفسه في سطر واحد في ملف القيود.

**لماذا يهم.** يُقرأ عند إعداد ملف القيود المحاسبية.

**القواعد.** نعم أو لا.
