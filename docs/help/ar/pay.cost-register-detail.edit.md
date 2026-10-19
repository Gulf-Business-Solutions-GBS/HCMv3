---
helpId: pay.cost-register-detail.edit
lang: ar
title: تفاصيل سجلات التكلفة — إنشاء / تحرير
entity: HCMv3.PayrollManagement.CostRegisterDetail
surfaces:
  - angular/src/app/admin/payroll-management/cost-register-details/create-or-edit-cost-register-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.CostRegisterDetails.Create]
---

## Overview

سطر تكلفة واحد: جزء من مبلغ نوع أجر يُرحَّل إلى حساب تحت مجموعة من عناصر التكلفة. ونوع الأجر الموزع على عدة حسابات أجور يعطي عدة أسطر، يحمل كل منها نسبة التكلفة من المبلغ.

## CostRegisterId {#CostRegisterDetail.CostRegisterId}
<!-- alias: loc:CostRegisterId, dom:CostRegisterDetail_CostRegisterId -->

**ما هو.** سجل التكلفة الذي يتبعه هذا السطر. ولا يظهر عند فتح النافذة من صفحة سجل التكلفة نفسه، إذ يُملأ تلقائياً.

**القواعد.** إلزامي. يُختار من قائمة سجلات التكلفة.

## WageTypeId {#CostRegisterDetail.WageTypeId}
<!-- alias: loc:WageTypeId, dom:CostRegisterDetail_WageTypeId -->

**ما هو.** نوع الأجر المحتسبة تكلفته. وتُحتسب تكلفة المتأخرات تحت نوع الأجر المصدر لها.

**مثال.** الراتب الأساسي

**القواعد.** إلزامي. يُختار من قائمة أنواع الأجور.

## AccountInfoId {#CostRegisterDetail.AccountInfoId}
<!-- alias: loc:AccountInfoId, dom:CostRegisterDetail_AccountInfoId -->

**ما هو.** الحساب الذي يُرحَّل إليه المبلغ.

**مثال.** 510100 -- الرواتب والأجور

**القواعد.** إلزامي. يُختار من قائمة بيانات الحسابات.

## Amount {#CostRegisterDetail.Amount}
<!-- alias: loc:Amount, dom:CostRegisterDetail_Amount -->

**ما هو.** المبلغ المُرحَّل.

**مثال.** `800.000`

**القواعد.** إلزامي. رقم.

## CurrencyId {#CostRegisterDetail.CurrencyId}
<!-- alias: loc:CurrencyId, dom:CostRegisterDetail_CurrencyId -->

**ما هو.** عملة الراتب الأساسي للموظف.

**مثال.** دينار كويتي

**القواعد.** اختياري. يُختار من قائمة العملات.

## CostElement1Id {#CostRegisterDetail.CostElement1Id}
<!-- alias: loc:CostElement1Id, dom:CostRegisterDetail_costElement1Id -->

**ما هو.** عنصر التكلفة 1 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة، منسوخاً من حساب الأجور للموظف. ويحمل الحقل اسم نوع عنصر التكلفة الذي رمزه CostElement1، ولا يعرض إلا عناصر التكلفة من ذلك النوع، ولا يظهر إطلاقاً إذا لم يوجد هذا النوع.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.

:::admin
تأتي تسميات الحقول من إدارة الهيكل التنظيمي ← أنواع عناصر التكلفة: أعطِ النوع الرمز CostElement1 إلى CostElement7 لتفعيل الحقل المقابل. والقيمة المحفوظة التي نُقل عنصر تكلفتها بعد ذلك إلى نوع آخر تبقى ظاهرة برقمها، فلا يمسحها فتح السجل.
:::

## CostElement2Id {#CostRegisterDetail.CostElement2Id}
<!-- alias: loc:CostElement2Id, dom:CostRegisterDetail_costElement2Id -->

**ما هو.** عنصر التكلفة 2 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة، منسوخاً من حساب الأجور للموظف. ويحمل الحقل اسم نوع عنصر التكلفة الذي رمزه CostElement2، ولا يعرض إلا عناصر التكلفة من ذلك النوع، ولا يظهر إطلاقاً إذا لم يوجد هذا النوع.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.

## CostElement3Id {#CostRegisterDetail.CostElement3Id}
<!-- alias: loc:CostElement3Id, dom:CostRegisterDetail_costElement3Id -->

**ما هو.** عنصر التكلفة 3 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة، منسوخاً من حساب الأجور للموظف. ويحمل الحقل اسم نوع عنصر التكلفة الذي رمزه CostElement3، ولا يعرض إلا عناصر التكلفة من ذلك النوع، ولا يظهر إطلاقاً إذا لم يوجد هذا النوع.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.

## CostElement4Id {#CostRegisterDetail.CostElement4Id}
<!-- alias: loc:CostElement4Id, dom:CostRegisterDetail_costElement4Id -->

**ما هو.** عنصر التكلفة 4 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة، منسوخاً من حساب الأجور للموظف. ويحمل الحقل اسم نوع عنصر التكلفة الذي رمزه CostElement4، ولا يعرض إلا عناصر التكلفة من ذلك النوع، ولا يظهر إطلاقاً إذا لم يوجد هذا النوع.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.

## CostElement5Id {#CostRegisterDetail.CostElement5Id}
<!-- alias: loc:CostElement5Id, dom:CostRegisterDetail_costElement5Id -->

**ما هو.** عنصر التكلفة 5 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة، منسوخاً من حساب الأجور للموظف. ويحمل الحقل اسم نوع عنصر التكلفة الذي رمزه CostElement5، ولا يعرض إلا عناصر التكلفة من ذلك النوع، ولا يظهر إطلاقاً إذا لم يوجد هذا النوع.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.

## CostElement6Id {#CostRegisterDetail.CostElement6Id}
<!-- alias: loc:CostElement6Id, dom:CostRegisterDetail_costElement6Id -->

**ما هو.** عنصر التكلفة 6 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة، منسوخاً من حساب الأجور للموظف. ويحمل الحقل اسم نوع عنصر التكلفة الذي رمزه CostElement6، ولا يعرض إلا عناصر التكلفة من ذلك النوع، ولا يظهر إطلاقاً إذا لم يوجد هذا النوع.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.

## CostElement7Id {#CostRegisterDetail.CostElement7Id}
<!-- alias: loc:CostElement7Id, dom:CostRegisterDetail_costElement7Id -->

**ما هو.** عنصر التكلفة 7 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة، منسوخاً من حساب الأجور للموظف. ويحمل الحقل اسم نوع عنصر التكلفة الذي رمزه CostElement7، ولا يعرض إلا عناصر التكلفة من ذلك النوع، ولا يظهر إطلاقاً إذا لم يوجد هذا النوع.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.
