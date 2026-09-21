---
helpId: pay.wage-account.edit
lang: ar
title: حسابات الأجور — إنشاء / تحرير
entity: HCMv3.PayrollManagement.WageAccount
surfaces:
  - angular/src/app/admin/payroll-management/wage-accounts/create-or-edit-wage-account-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.WageAccounts.Create]
---

## Overview

ربط على مستوى الشركة بين نوع أجر وحساب في دفتر الأستاذ، اعتباراً من تاريخ معيّن، مع حسابه المقابل وعناصر التكلفة.

لا يُقرأ حالياً: يأخذ محرك التكلفة وقيد اليومية الحسابات من **حسابات الأجور** الخاصة بكل موظف (في سجل الموظف)، ولا يقرأ شيءٌ هذه القائمة. ونوع الأجر الذي ليس له حساب أجور للموظف في الفترة يوقف احتساب التكلفة لذلك الموظف، مهما كان المضبوط هنا.

## WageTypeId {#WageAccount.WageTypeId}
<!-- alias: loc:WageTypeId, dom:WageAccount_WageTypeId -->

**ما هو.** نوع الأجر الذي يخصه هذا الربط.

**مثال.** بدل السكن

**القواعد.** إلزامي. يُختار من قائمة أنواع الأجور.

## AccountInfoId {#WageAccount.AccountInfoId}
<!-- alias: loc:AccountInfoId, dom:WageAccount_AccountInfoId -->

**ما هو.** الحساب الذي يُرحَّل إليه نوع الأجر.

**مثال.** 510100 -- الرواتب والأجور

**القواعد.** إلزامي. يُختار من قائمة بيانات الحسابات.

## OffsetAccountInfoId {#WageAccount.OffsetAccountInfoId}
<!-- alias: loc:OffsetAccountInfoId, dom:WageAccount_OffsetAccountInfoId -->

**ما هو.** الحساب المقابل للقيد.

**مثال.** 210500 -- رواتب مستحقة الدفع

**القواعد.** اختياري. يُختار من قائمة بيانات الحسابات.

## CostPercentage {#WageAccount.CostPercentage}
<!-- alias: loc:CostPercentage, dom:WageAccount_CostPercentage -->

**ما هو.** حصة مبلغ نوع الأجر التي يحملها هذا الربط.

**مثال.** `100`

**القواعد.** اختياري. رقم.

## CostElement1Id {#WageAccount.CostElement1Id}
<!-- alias: loc:CostElement1Id, dom:WageAccount_costElement1Id -->

**ما هو.** عنصر التكلفة 1 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.

## CostElement2Id {#WageAccount.CostElement2Id}
<!-- alias: loc:CostElement2Id, dom:WageAccount_costElement2Id -->

**ما هو.** عنصر التكلفة 2 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.

## CostElement3Id {#WageAccount.CostElement3Id}
<!-- alias: loc:CostElement3Id, dom:WageAccount_costElement3Id -->

**ما هو.** عنصر التكلفة 3 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.

## CostElement4Id {#WageAccount.CostElement4Id}
<!-- alias: loc:CostElement4Id, dom:WageAccount_costElement4Id -->

**ما هو.** عنصر التكلفة 4 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.

## CostElement5Id {#WageAccount.CostElement5Id}
<!-- alias: loc:CostElement5Id, dom:WageAccount_costElement5Id -->

**ما هو.** عنصر التكلفة 5 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.

## CostElement6Id {#WageAccount.CostElement6Id}
<!-- alias: loc:CostElement6Id, dom:WageAccount_costElement6Id -->

**ما هو.** عنصر التكلفة 6 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.

## CostElement7Id {#WageAccount.CostElement7Id}
<!-- alias: loc:CostElement7Id, dom:WageAccount_costElement7Id -->

**ما هو.** عنصر التكلفة 7 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.
