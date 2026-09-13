---
helpId: pa.employee-on-hold.edit
lang: ar
title: حالات الإيقاف للموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeOnHold
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/on-holds/create-or-edit-employee-on-hold-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeOnHolds.Create]
---

## Overview

يوقف صرف جزء من أجر الموظف أو كله خلال فترة. ولا يجوز أن تتداخل فترات إيقاف الموظف نفسه.

تطبّق الرواتب الإيقاف الساري في تاريخ الدفع. وإيقاف الكل يفعّل كل خيارات الإيقاف دفعة واحدة.

## BankTransfer {#EmployeeOnHold.BankTransfer}
<!-- alias: loc:BankTransfer, dom:EmployeeOnHold_BankTransfer -->

**ما هو.** هل يُوقف تحويل راتب الموظف.

**مثال.** إيقاف

**القواعد.** إلزامي. إحدى القيم: لا ينطبق، إيقاف، إلغاء الإيقاف.

:::admin
من بين صيغ ملفات البنك، لا تستبعد الموظفين الموقوف تحويلهم إلا صيغة البنك الأهلي الكويتي (ABK). أما الصيغ الأخرى فلا تتحقق منه حالياً.
:::

## EmployeeActionId {#EmployeeOnHold.EmployeeActionId}
<!-- alias: loc:EmployeeActionId, dom:EmployeeOnHold_EmployeeActionId -->

**ما هو.** الإجراء الشخصي الذي ينتمي إليه هذا الإيقاف، إن وُجد.

**لماذا يهم.** يُسجَّل كرابط فقط، ولا تقرؤه الرواتب.

**القواعد.** اختياري. يُختار من قائمة.

## OnHold {#EmployeeOnHold.OnHold}
<!-- alias: loc:OnHold, dom:EmployeeOnHold_OnHold -->

**ما هو.** يوقف كل شيء.

**لماذا يهم.** تعامل الرواتب كل خيارات الإيقاف أدناه على أنها محددة، ويُدرج تقرير الموظفين غير المشمولين بالرواتب الموظف.

**القواعد.** نعم أو لا.

## OnHoldBasicPay {#EmployeeOnHold.OnHoldBasicPay}
<!-- alias: loc:OnHoldBasicPay, dom:EmployeeOnHold_OnHoldBasicPay -->

**ما هو.** يوقف الأجر الأساسي.

**لماذا يهم.** لا تصرف الرواتب أجراً أساسياً ما دام محدداً.

**القواعد.** نعم أو لا.

## OnHoldLeavePay {#EmployeeOnHold.OnHoldLeavePay}
<!-- alias: loc:OnHoldLeavePay, dom:EmployeeOnHold_OnHoldLeavePay -->

**ما هو.** يوقف أجر الإجازات.

**القواعد.** نعم أو لا.

## OnHoldIndemnityPay {#EmployeeOnHold.OnHoldIndemnityPay}
<!-- alias: loc:OnHoldIndemnityPay, dom:EmployeeOnHold_OnHoldIndemnityPay -->

**ما هو.** يوقف صرف مكافأة نهاية الخدمة.

**القواعد.** نعم أو لا.

## OnHoldAirTicketPay {#EmployeeOnHold.OnHoldAirTicketPay}
<!-- alias: loc:OnHoldAirTicketPay, dom:EmployeeOnHold_OnHoldAirTicketPay -->

**ما هو.** يوقف صرف تذاكر الطيران.

**لماذا يهم.** يُسجَّل فقط؛ ولا تتحقق منه الرواتب حالياً عند صرف التذاكر.

**القواعد.** نعم أو لا.

## OnHoldLoanDeduction {#EmployeeOnHold.OnHoldLoanDeduction}
<!-- alias: loc:OnHoldLoanDeduction, dom:EmployeeOnHold_OnHoldLoanDeduction -->

**ما هو.** يوقف اقتطاع أقساط القروض.

**لماذا يهم.** لا تسترد الرواتب القروض ما دام محدداً.

**القواعد.** نعم أو لا.

## OnHoldAllEarnings {#EmployeeOnHold.OnHoldAllEarnings}
<!-- alias: loc:OnHoldAllEarnings, dom:EmployeeOnHold_OnHoldAllEarnings -->

**ما هو.** يوقف جميع الاستحقاقات.

**القواعد.** نعم أو لا.

## OnHoldAllDeductions {#EmployeeOnHold.OnHoldAllDeductions}
<!-- alias: loc:OnHoldAllDeductions, dom:EmployeeOnHold_OnHoldAllDeductions -->

**ما هو.** يوقف جميع الاقتطاعات.

**القواعد.** نعم أو لا.
