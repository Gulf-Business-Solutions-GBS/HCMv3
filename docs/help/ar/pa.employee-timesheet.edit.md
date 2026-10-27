---
helpId: pa.employee-timesheet.edit
lang: ar
title: كشوف دوام الموظف — إنشاء / تحرير
entity: HCMv3.TimeManagement.EmployeeTimesheet
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/timesheets/create-or-edit-employee-timesheet-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.EmployeeTimesheets.Create]
---

## Overview

كشف دوام يدوي للموظف في تاريخ واحد: أوقات الدخول والخروج والاستراحة الفعلية. كشف واحد لكل موظف في كل تاريخ.

للموظفين الذين تأخذ سياسة الوقت الخاصة بهم البصمات من كشوف الدوام، يبني محرك الوقت أوقات الدخول والاستراحة لليوم من هذه القيود، ويقرأ إجمالي الساعات.

## TransDate {#EmployeeTimesheet.TransDate}
<!-- alias: loc:TransDate, dom:EmployeeTimesheet_TransDate -->

**ما هو.** التاريخ الذي يخصه كشف الدوام.

**مثال.** 10 سبتمبر 2026

**القواعد.** إلزامي. تاريخ.

:::admin
عند بناء اليوم من كشوف الدوام، يبحث محرك الوقت عن الكشف بالتاريخ فقط لا بالموظف. لذلك قد يُعطى الموظف أوقات الدخول والاستراحة الخاصة بموظف آخر في تاريخ فيه كشوف لأكثر من موظف، ولا يتأثر إجمالي الساعات. وقد أُبقي على هذا السلوك عمداً مطابقةً للنظام السابق، لذا يُرجى مراجعة أوقات الدخول والاستراحة في تلك التواريخ.
:::

## ActualStartTime {#EmployeeTimesheet.ActualStartTime}
<!-- alias: loc:ActualStartTime, dom:EmployeeTimesheet_ActualStartTime -->

**ما هو.** الوقت الذي بدأ فيه الموظف العمل.

**مثال.** 07:55

**لماذا يهم.** يُستخدم وقتاً فعلياً للدخول في اليوم.

**القواعد.** اختياري. وقت من اليوم.

## ActualEndTime {#EmployeeTimesheet.ActualEndTime}
<!-- alias: loc:ActualEndTime, dom:EmployeeTimesheet_ActualEndTime -->

**ما هو.** الوقت الذي أنهى فيه الموظف العمل.

**مثال.** 16:10

**لماذا يهم.** يُسجَّل مع الكشف، ولا يقرؤه محرك الوقت حالياً.

**القواعد.** اختياري. وقت من اليوم.

## BreakStartTime {#EmployeeTimesheet.BreakStartTime}
<!-- alias: loc:BreakStartTime, dom:EmployeeTimesheet_BreakStartTime -->

**ما هو.** وقت بدء الاستراحة.

**مثال.** 12:00

**القواعد.** اختياري. وقت من اليوم.

## BreakEndTime {#EmployeeTimesheet.BreakEndTime}
<!-- alias: loc:BreakEndTime, dom:EmployeeTimesheet_BreakEndTime -->

**ما هو.** وقت انتهاء الاستراحة.

**مثال.** 12:30

**القواعد.** اختياري. وقت من اليوم.

## TotalHours {#EmployeeTimesheet.TotalHours}
<!-- alias: loc:TotalHours, dom:EmployeeTimesheet_TotalHours -->

**ما هو.** إجمالي ساعات العمل في التاريخ.

**مثال.** 7.75

**لماذا يهم.** يقرؤه محرك الوقت لهذا الموظف وهذا التاريخ.

**القواعد.** اختياري. رقم.

## Approved {#EmployeeTimesheet.Approved}
<!-- alias: loc:Approved, dom:EmployeeTimesheet_Approved -->

**ما هو.** علامة اعتماد تُسجَّل مع الكشف.

**لماذا يهم.** تُسجَّل فقط، ولا يتحقق منها محرك الوقت حالياً.

**القواعد.** اختياري. رقم صحيح.
