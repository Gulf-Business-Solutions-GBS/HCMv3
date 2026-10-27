---
helpId: pay.employee-sub-group-benefit-detail.edit
lang: ar
title: تفاصيل مزايا المجموعة الفرعية للموظفين — إنشاء / تحرير
entity: HCMv3.PayrollManagement.EmployeeSubGroupBenefitDetail
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/benefits/benefit-details/create-or-edit-employee-sub-group-benefit-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupBenefitDetails.Create]
---

## Overview

مبلغ مزايا الدرجة لكل نوع من العائلين، مع حد للعمر.

لا تُقرأ حالياً: لا يستخدم نظام الرواتب هذه القائمة، وهي محفوظة للرجوع إليها فقط.

## DependantType {#EmployeeSubGroupBenefitDetail.DependantTypeId}
<!-- alias: loc:DependantType, dom:EmployeeSubGroupBenefitDetail_DependantTypeId -->

**ما هو.** نوع العائل.

**مثال.** ابن/ابنة

**القواعد.** اختياري. يُختار من قائمة أنواع صفات العائل.

## Amount {#EmployeeSubGroupBenefitDetail.Amount}
<!-- alias: loc:Amount, dom:EmployeeSubGroupBenefitDetail_Amount -->

**ما هو.** المبلغ لكل عائل من ذلك النوع.

**مثال.** `500`

**القواعد.** اختياري. رقم.

## AgeLimit {#EmployeeSubGroupBenefitDetail.AgeLimit}
<!-- alias: loc:AgeLimit, dom:EmployeeSubGroupBenefitDetail_AgeLimit -->

**ما هو.** أكبر عمر يُحتسب فيه العائل.

**مثال.** `21`

**القواعد.** اختياري. رقم.
