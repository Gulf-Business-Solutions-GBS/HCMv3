---
helpId: pay.salary-register-detail.edit
lang: ar
title: تفاصيل سجلات الرواتب — إنشاء / تحرير
entity: HCMv3.PayrollManagement.SalaryRegisterDetail
surfaces:
  - angular/src/app/admin/payroll-management/salary-register-details/create-or-edit-salary-register-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SalaryRegisterDetails.Create]
---

## Overview

سطر واحد من كشف الراتب: نوع أجر مع أجره وعدده ومبلغه. ولا يمكن تعديل أسطر كشف الراتب المقفل.

## SalaryRegisterId {#SalaryRegisterDetail.SalaryRegisterId}
<!-- alias: loc:SalaryRegisterId, dom:SalaryRegisterDetail_SalaryRegisterId -->

**ما هو.** كشف الراتب الذي يتبعه هذا السطر.

**القواعد.** إلزامي. يُختار من قائمة سجلات الرواتب.

## WageTypeId {#SalaryRegisterDetail.WageTypeId}
<!-- alias: loc:WageTypeId, dom:SalaryRegisterDetail_WageTypeId -->

**ما هو.** نوع الأجر لهذا السطر.

**مثال.** الراتب الأساسي

**القواعد.** إلزامي. يُختار من قائمة أنواع الأجور.

## Rate {#SalaryRegisterDetail.Rate}
<!-- alias: loc:Rate, dom:SalaryRegisterDetail_Rate -->

**ما هو.** الأجر الشهري الذي احتُسب منه السطر.

**مثال.** `800`

**القواعد.** إلزامي. رقم.

## Number {#SalaryRegisterDetail.Number}
<!-- alias: loc:Number, dom:SalaryRegisterDetail_Number -->

**ما هو.** الكمية -- أيام أو ساعات أو وحدات -- التي احتُسب عليها السطر.

**مثال.** `30`

**القواعد.** إلزامي. رقم.

## Amount {#SalaryRegisterDetail.Amount}
<!-- alias: loc:Amount, dom:SalaryRegisterDetail_Amount -->

**ما هو.** مبلغ السطر.

**مثال.** `800.000`

**القواعد.** إلزامي. رقم.
