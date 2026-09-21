---
helpId: pay.wage-acc-group-detail.edit
lang: ar
title: تفاصيل مجموعات حسابات الأجور — إنشاء / تحرير
entity: HCMv3.PayrollManagement.WageAccGroupDetail
surfaces:
  - angular/src/app/admin/payroll-management/wage-acc-group-details/create-or-edit-wage-acc-group-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.WageAccGroupDetails.Create]
---

## Overview

سطر واحد من مجموعة حسابات الأجور: الحساب الذي يُرحَّل إليه نوع الأجر، وحسابه المقابل وعناصر التكلفة.

لا تُقرأ حالياً: يأخذ محرك التكلفة وقيد اليومية الحسابات من حسابات الأجور الخاصة بكل موظف، ولا يقرأ شيءٌ هذه الأسطر. وهي محفوظة للرجوع إليها ولاستخدام مستقبلي.

## WageAccGroupId {#WageAccGroupDetail.WageAccGroupId}
<!-- alias: loc:WageAccGroupId, dom:WageAccGroupDetail_WageAccGroupId -->

**ما هو.** مجموعة حسابات الأجور التي يتبعها هذا السطر.

**مثال.** حسابات المكتب الرئيسي

**القواعد.** إلزامي. يُختار من قائمة مجموعات حسابات الأجور.

## WageTypeId {#WageAccGroupDetail.WageTypeId}
<!-- alias: loc:WageTypeId, dom:WageAccGroupDetail_WageTypeId -->

**ما هو.** نوع الأجر الذي يربطه هذا السطر.

**مثال.** الراتب الأساسي

**القواعد.** إلزامي. يُختار من قائمة أنواع الأجور.

## AccountInfoId {#WageAccGroupDetail.AccountInfoId}
<!-- alias: loc:AccountInfoId, dom:WageAccGroupDetail_AccountInfoId -->

**ما هو.** الحساب الذي يُرحَّل إليه نوع الأجر.

**مثال.** 510100 -- الرواتب والأجور

**القواعد.** إلزامي. يُختار من قائمة بيانات الحسابات.

## OffsetAccountInfoType {#WageAccGroupDetail.OffsetAccountInfoType}
<!-- alias: loc:OffsetAccountInfoType, dom:WageAccGroupDetail_OffsetAccountInfoType -->

**ما هو.** مصدر الحساب المقابل: حساب ثابت يُختار أدناه، أو حساب يُستخرج من رمز الموظف.

**مثال.** ثابت

**القواعد.** إلزامي. إحدى القيم: ثابت، رقم الموظف.

## OffsetAccountInfoId {#WageAccGroupDetail.OffsetAccountInfoId}
<!-- alias: loc:OffsetAccountInfoId, dom:WageAccGroupDetail_OffsetAccountInfoId -->

**ما هو.** الحساب المقابل، إذا كان نوع الحساب المقابل ثابتاً.

**مثال.** 210500 -- رواتب مستحقة الدفع

**القواعد.** اختياري. يُختار من قائمة بيانات الحسابات.

## CostPercentage {#WageAccGroupDetail.CostPercentage}
<!-- alias: loc:CostPercentage, dom:WageAccGroupDetail_CostPercentage -->

**ما هو.** حصة مبلغ نوع الأجر التي يحملها هذا السطر.

**مثال.** `100`

**القواعد.** اختياري. رقم.

## MergeOffsetAccount {#WageAccGroupDetail.MergeOffsetAccount}
<!-- alias: loc:MergeOffsetAccount, dom:WageAccGroupDetail_MergeOffsetAccount -->

**ما هو.** ما إذا كانت أسطر الحساب المقابل لهذا الربط تُدمج في سطر واحد.

**القواعد.** نعم أو لا.

## CostElement1Id {#WageAccGroupDetail.CostElement1Id}
<!-- alias: loc:CostElement1Id, dom:WageAccGroupDetail_costElement1Id -->

**ما هو.** عنصر التكلفة 1 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.

## CostElement2Id {#WageAccGroupDetail.CostElement2Id}
<!-- alias: loc:CostElement2Id, dom:WageAccGroupDetail_costElement2Id -->

**ما هو.** عنصر التكلفة 2 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.

## CostElement3Id {#WageAccGroupDetail.CostElement3Id}
<!-- alias: loc:CostElement3Id, dom:WageAccGroupDetail_costElement3Id -->

**ما هو.** عنصر التكلفة 3 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.

## CostElement4Id {#WageAccGroupDetail.CostElement4Id}
<!-- alias: loc:CostElement4Id, dom:WageAccGroupDetail_costElement4Id -->

**ما هو.** عنصر التكلفة 4 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.

## CostElement5Id {#WageAccGroupDetail.CostElement5Id}
<!-- alias: loc:CostElement5Id, dom:WageAccGroupDetail_costElement5Id -->

**ما هو.** عنصر التكلفة 5 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.

## CostElement6Id {#WageAccGroupDetail.CostElement6Id}
<!-- alias: loc:CostElement6Id, dom:WageAccGroupDetail_costElement6Id -->

**ما هو.** عنصر التكلفة 6 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.

## CostElement7Id {#WageAccGroupDetail.CostElement7Id}
<!-- alias: loc:CostElement7Id, dom:WageAccGroupDetail_costElement7Id -->

**ما هو.** عنصر التكلفة 7 -- أحد أبعاد المحاسبة التي يصل عددها إلى سبعة.

**القواعد.** اختياري. يُختار من قائمة عناصر التكلفة.
