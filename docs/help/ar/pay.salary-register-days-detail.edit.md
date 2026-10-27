---
helpId: pay.salary-register-days-detail.edit
lang: ar
title: تفاصيل أيام سجلات الرواتب — إنشاء / تحرير
entity: HCMv3.PayrollManagement.SalaryRegisterDaysDetail
surfaces:
  - angular/src/app/admin/payroll-management/salary-register-days-details/create-or-edit-salary-register-days-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SalaryRegisterDaysDetails.Create]
---

## Overview

تفصيل سطر من كشف الراتب بحسب التاريخ -- العمل الإضافي يوماً بيوم مثلاً، أو الإجازة المدفوعة عبر أشهر. ولا يمكن تعديل تفاصيل كشف الراتب المقفل.

## SalaryRegisterDetailId {#SalaryRegisterDaysDetail.SalaryRegisterDetailId}
<!-- alias: loc:SalaryRegisterDetailId, dom:SalaryRegisterDaysDetail_SalaryRegisterDetailId -->

**ما هو.** سطر كشف الراتب الذي يتبعه هذا التفصيل.

**القواعد.** إلزامي. يُختار من قائمة تفاصيل سجلات الرواتب.

## Period {#SalaryRegisterDaysDetail.Period}
<!-- alias: loc:Period, dom:SalaryRegisterDaysDetail_Period -->

**ما هو.** التاريخ الذي يخصه هذا الجزء من السطر.

**مثال.** `2026-03-14`

**القواعد.** إلزامي. تاريخ.

## Rate {#SalaryRegisterDaysDetail.Rate}
<!-- alias: loc:Rate, dom:SalaryRegisterDaysDetail_Rate -->

**ما هو.** الأجر لذلك التاريخ.

**مثال.** `800`

**القواعد.** إلزامي. رقم.

## Number {#SalaryRegisterDaysDetail.Number}
<!-- alias: loc:Number, dom:SalaryRegisterDaysDetail_Number -->

**ما هو.** الأيام أو الساعات أو الوحدات لذلك التاريخ.

**مثال.** `2`

**القواعد.** إلزامي. رقم.

## Amount {#SalaryRegisterDaysDetail.Amount}
<!-- alias: loc:Amount, dom:SalaryRegisterDaysDetail_Amount -->

**ما هو.** المبلغ لذلك التاريخ.

**مثال.** `12.500`

**القواعد.** إلزامي. رقم.
