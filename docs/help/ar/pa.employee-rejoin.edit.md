---
helpId: pa.employee-rejoin.edit
lang: ar
title: حالات إعادة التحاق الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeRejoin
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/rejoins/create-or-edit-employee-rejoin-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeRejoins.Create]
---

## Overview

يسجل عودة موظف أُنهيت خدمته.

بعد بلوغ تاريخ العودة، لا تأخذ الرواتب في الحسبان إلا حالات الفصل التي يقع آخر يوم فيها في تاريخ العودة أو بعده، فلا يعود الخروج السابق يوقف الأجر. أما خيارات الترحيل فتُسجَّل للرجوع إليها؛ ولا تُرحَّل الأرصدة تلقائياً.

## RejoinDate {#EmployeeRejoin.RejoinDate}
<!-- alias: loc:RejoinDate, dom:EmployeeRejoin_RejoinDate -->

**ما هو.** تاريخ عودة الموظف.

**مثال.** 1 فبراير 2027

**لماذا يهم.** اعتباراً من هذا التاريخ تتجاهل الرواتب حالات الفصل التي انتهت قبله.

**القواعد.** إلزامي. تاريخ.

## CarryForwardLeaveBalance {#EmployeeRejoin.CarryForwardLeaveBalance}
<!-- alias: loc:CarryForwardLeaveBalance, dom:EmployeeRejoin_CarryForwardLeaveBalance -->

**ما هو.** هل يُرحَّل رصيد الإجازات.

**لماذا يهم.** يُسجَّل فقط، ولا يُرحَّل الرصيد تلقائياً.

**القواعد.** نعم أو لا.

## CarryForwardTicketBalance {#EmployeeRejoin.CarryForwardTicketBalance}
<!-- alias: loc:CarryForwardTicketBalance, dom:EmployeeRejoin_CarryForwardTicketBalance -->

**ما هو.** هل يُرحَّل رصيد التذاكر.

**لماذا يهم.** يُسجَّل فقط، ولا يُرحَّل الرصيد تلقائياً.

**القواعد.** نعم أو لا.

## CarryForwardIndemnityBalance {#EmployeeRejoin.CarryForwardIndemnityBalance}
<!-- alias: loc:CarryForwardIndemnityBalance, dom:EmployeeRejoin_CarryForwardIndemnityBalance -->

**ما هو.** هل يُرحَّل رصيد مكافأة نهاية الخدمة.

**لماذا يهم.** يُسجَّل فقط، ولا يُرحَّل الرصيد تلقائياً.

**القواعد.** نعم أو لا.

## CarryForwardLoanBalance {#EmployeeRejoin.CarryForwardLoanBalance}
<!-- alias: loc:CarryForwardLoanBalance, dom:EmployeeRejoin_CarryForwardLoanBalance -->

**ما هو.** هل يُرحَّل رصيد القروض.

**لماذا يهم.** يُسجَّل فقط، ولا يُرحَّل الرصيد تلقائياً.

**القواعد.** نعم أو لا.
