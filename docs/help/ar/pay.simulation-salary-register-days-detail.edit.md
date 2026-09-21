---
helpId: pay.simulation-salary-register-days-detail.edit
lang: ar
title: تفاصيل أيام سجلات الرواتب التجريبية — إنشاء / تحرير
entity: HCMv3.PayrollManagement.SimulationSalaryRegisterDaysDetail
surfaces:
  - angular/src/app/admin/payroll-management/simulation-salary-register-days-details/create-or-edit-simulation-salary-register-days-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SimulationSalaryRegisterDaysDetails.Create]
---

## Overview

تفصيل سطر من كشف راتب المحاكاة بحسب التاريخ.

## SimulationSalaryRegisterDetailId {#SimulationSalaryRegisterDaysDetail.SimulationSalaryRegisterDetailId}
<!-- alias: loc:SimulationSalaryRegisterDetailId, dom:SimulationSalaryRegisterDaysDetail_SimulationSalaryRegisterDetailId -->

**ما هو.** سطر كشف راتب المحاكاة الذي يتبعه هذا التفصيل.

**القواعد.** إلزامي. يُختار من قائمة تفاصيل سجلات الرواتب التجريبية.

## Period {#SimulationSalaryRegisterDaysDetail.Period}
<!-- alias: loc:Period, dom:SimulationSalaryRegisterDaysDetail_Period -->

**ما هو.** التاريخ الذي يخصه هذا الجزء من السطر.

**مثال.** `2026-03-14`

**القواعد.** إلزامي. تاريخ.

## Rate {#SimulationSalaryRegisterDaysDetail.Rate}
<!-- alias: loc:Rate, dom:SimulationSalaryRegisterDaysDetail_Rate -->

**ما هو.** الأجر لذلك التاريخ.

**مثال.** `800`

**القواعد.** إلزامي. رقم.

## Number {#SimulationSalaryRegisterDaysDetail.Number}
<!-- alias: loc:Number, dom:SimulationSalaryRegisterDaysDetail_Number -->

**ما هو.** الأيام أو الساعات أو الوحدات لذلك التاريخ.

**مثال.** `2`

**القواعد.** إلزامي. رقم.

## Amount {#SimulationSalaryRegisterDaysDetail.Amount}
<!-- alias: loc:Amount, dom:SimulationSalaryRegisterDaysDetail_Amount -->

**ما هو.** المبلغ لذلك التاريخ.

**مثال.** `12.500`

**القواعد.** إلزامي. رقم.
