---
helpId: sys.payroll-driver.edit
lang: ar
title: محركات الرواتب — إنشاء / تحرير
entity: HCMv3.SystemSettings.PayrollDriver
surfaces:
  - angular/src/app/admin/system-settings/payroll-drivers/create-or-edit-payroll-driver-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.PayrollDrivers.Create]
---

## Overview

إعداد رواتب لكل بلد. لا يُقرأ حالياً: يُؤخذ سلوك خصم الإجازة الذي يحمل اسمه من **إعدادات الوقت**، ولا شيء يقرأ هذه القائمة.

## CountryId {#PayrollDriver.CountryId}
<!-- alias: loc:CountryId, dom:PayrollDriver_CountryId -->

**ما هو.** البلد الذي يخصه الإعداد.

**مثال.** الكويت

**القواعد.** إلزامي. يُختار من قائمة.

## LeaveDeductionStartMonth {#PayrollDriver.LeaveDeductionStartMonth}
<!-- alias: loc:LeaveDeductionStartMonth, dom:PayrollDriver_LeaveDeductionStartMonth -->

**ما هو.** ما إذا كانت الإجازة تُخصم من الشهر الذي تبدأ فيه.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.
