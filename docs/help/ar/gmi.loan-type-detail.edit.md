---
helpId: gmi.loan-type-detail.edit
lang: ar
title: تفاصيل أنواع القروض — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.LoanTypeDetail
surfaces:
  - angular/src/app/admin/general-master-info/loan-type-details/create-or-edit-loan-type-detail-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.LoanTypeDetails.Create]
---

## Overview

نسب مرتبطة بنوع قرض. لا تُستخدم حالياً: فهي تخص الاسترداد المتغير للقرض، الذي لا يدعمه احتساب الرواتب بعدُ.

## LoanType {#LoanTypeDetail.LoanTypeId}
<!-- alias: loc:LoanType, dom:LoanTypeDetail_LoanTypeId -->

**ما هو.** نوع القرض الذي تنتمي إليه النسبة.

**القواعد.** اختياري. يُختار من قائمة أنواع القروض.

## WageType {#LoanTypeDetail.WageTypeId}
<!-- alias: loc:WageType, dom:LoanTypeDetail_WageTypeId -->

**ما هو.** بند الأجر الذي تُحتسب منه النسبة في هذا السطر، للقروض التي نوع خصمها «متغير».

**مثال.** 1001 - الراتب الأساسي، بنسبة 25

**لماذا يهم.** في القرض المتغير، يساوي القسط الشهري مجموع مبلغ نوع الأجر هذا في كشف راتب الموظف مضروباً في نسبة السطر، لجميع الأسطر، على ألا يتجاوز الرصيد المتبقي. ونوع الأجر الذي لا يُصرف للموظف في ذلك الشهر لا يضيف شيئاً. ولا يُدرج نوع الأجر إلا مرة واحدة لكل نوع قرض، ويجب أن تكون النسبة أكبر من 0 ولا تزيد على 100.

**القواعد.** إلزامي. يُختار من قائمة أنواع الأجور.

## Percent {#LoanTypeDetail.Percent}
<!-- alias: loc:Percent, dom:LoanTypeDetail_Percent -->

**ما هو.** النسبة.

**القواعد.** إلزامي. رقم.
