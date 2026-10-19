---
helpId: pay.simulation-salary-register-detail.edit
lang: ar
title: تفاصيل سجلات الرواتب التجريبية — إنشاء / تحرير
entity: HCMv3.PayrollManagement.SimulationSalaryRegisterDetail
surfaces:
  - angular/src/app/admin/payroll-management/simulation-salary-register-details/create-or-edit-simulation-salary-register-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SimulationSalaryRegisterDetails.Create]
---

## Overview

سطر واحد من كشف راتب المحاكاة: نوع أجر مع أجره وعدده ومبلغه.

## SimulationSalaryRegisterId {#SimulationSalaryRegisterDetail.SimulationSalaryRegisterId}
<!-- alias: loc:SimulationSalaryRegisterId, dom:SimulationSalaryRegisterDetail_SimulationSalaryRegisterId -->

**ما هو.** كشف راتب المحاكاة الذي يتبعه هذا السطر. ولا يظهر عند فتح النافذة من صفحة كشف المحاكاة نفسه، إذ يُملأ تلقائياً.

**القواعد.** إلزامي. يُختار من قائمة سجلات الرواتب التجريبية.

## WageTypeId {#SimulationSalaryRegisterDetail.WageTypeId}
<!-- alias: loc:WageTypeId, dom:SimulationSalaryRegisterDetail_WageTypeId -->

**ما هو.** نوع الأجر لهذا السطر.

**مثال.** الراتب الأساسي

**القواعد.** إلزامي. يُختار من قائمة أنواع الأجور.

## Rate {#SimulationSalaryRegisterDetail.Rate}
<!-- alias: loc:Rate, dom:SimulationSalaryRegisterDetail_Rate -->

**ما هو.** الأجر الشهري الذي احتُسب منه السطر.

**مثال.** `800`

**القواعد.** إلزامي. رقم.

## Number {#SimulationSalaryRegisterDetail.Number}
<!-- alias: loc:Number, dom:SimulationSalaryRegisterDetail_Number -->

**ما هو.** الكمية -- أيام أو ساعات أو وحدات -- التي احتُسب عليها السطر.

**مثال.** `30`

**القواعد.** إلزامي. رقم.

## Amount {#SimulationSalaryRegisterDetail.Amount}
<!-- alias: loc:Amount, dom:SimulationSalaryRegisterDetail_Amount -->

**ما هو.** مبلغ السطر.

**مثال.** `800.000`

**القواعد.** إلزامي. رقم.
