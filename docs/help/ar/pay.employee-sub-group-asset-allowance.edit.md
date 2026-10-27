---
helpId: pay.employee-sub-group-asset-allowance.edit
lang: ar
title: بدلات عهد المجموعة الفرعية للموظفين — إنشاء / تحرير
entity: HCMv3.PayrollManagement.EmployeeSubGroupAssetAllowance
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/asset-allowances/create-or-edit-employee-sub-group-asset-allowance-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupAssetAllowances.Create]
---

## Overview

بدل عن أصول الشركة، يُصرف عبر نوع أجر.

لا تُقرأ حالياً: لا يستخدم نظام الرواتب هذه القائمة، وهي محفوظة للرجوع إليها فقط.

## WageType {#EmployeeSubGroupAssetAllowance.WageTypeId}
<!-- alias: loc:WageType, dom:EmployeeSubGroupAssetAllowance_WageTypeId -->

**ما هو.** نوع أجر البدل.

**مثال.** بدل سيارة

**لماذا يهم.** لا يُدرج نوع الأجر إلا مرة واحدة لكل مجموعة فرعية.

**القواعد.** اختياري. يُختار من قائمة.

## Amount {#EmployeeSubGroupAssetAllowance.Amount}
<!-- alias: loc:Amount, dom:EmployeeSubGroupAssetAllowance_Amount -->

**ما هو.** مبلغ البدل.

**مثال.** `100`

**القواعد.** اختياري. رقم.
