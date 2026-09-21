---
helpId: pay.temp-employee-benefit.edit
lang: ar
title: مزايا الموظف المؤقتة — إنشاء / تحرير
entity: HCMv3.PayrollManagement.TempEmployeeBenefit
surfaces:
  - angular/src/app/admin/payroll-management/temp-employee-benefits/create-or-edit-temp-employee-benefit-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.TempEmployeeBenefits.Create]
---

## Overview

الدرجة التي يتبعها الموظف في المزايا. ولا يكون للموظف إلا درجة واحدة.

لا يُقرأ حالياً: فهو يربط الموظف بورقة الدرجة المرجعية، ولا يستخدمه نظام الرواتب.

## EmployeeId {#TempEmployeeBenefit.EmployeeId}
<!-- alias: loc:EmployeeId, dom:TempEmployeeBenefit_EmployeeId -->

**ما هو.** الموظف.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## TempBenefitTypeId {#TempEmployeeBenefit.TempBenefitTypeId}
<!-- alias: loc:TempBenefitTypeId, dom:TempEmployeeBenefit_TempBenefitTypeId -->

**ما هو.** درجة المزايا.

**مثال.** الدرجة 5

**القواعد.** إلزامي. يُختار من قائمة أنواع المزايا المؤقتة.
