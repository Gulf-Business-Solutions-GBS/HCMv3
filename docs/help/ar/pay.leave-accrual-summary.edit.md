---
helpId: pay.leave-accrual-summary.edit
lang: ar
title: ملخصات تراكم الإجازات — إنشاء / تحرير
entity: HCMv3.PayrollManagement.LeaveAccrualSummary
surfaces:
  - angular/src/app/admin/payroll-management/leave-accrual-summaries/create-or-edit-leave-accrual-summary-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.LeaveAccrualSummaries.Create]
---

## Overview

لقطة شهرية لرصيد إجازة الموظف لنوع إجازة واحد.

لا تُكتب حالياً: لا توجد عملية تملأ هذه الملخصات، ولا شيء يقرؤها. فالأرصدة تُحتسب عند الحاجة إليها من سجل الوقت وسجلات الموظف.

## EmployeeId {#LeaveAccrualSummary.EmployeeId}
<!-- alias: loc:EmployeeId, dom:LeaveAccrualSummary_EmployeeId -->

**ما هو.** الموظف.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## AbsenceTypeId {#LeaveAccrualSummary.AbsenceTypeId}
<!-- alias: loc:AbsenceTypeId, dom:LeaveAccrualSummary_AbsenceTypeId -->

**ما هو.** نوع الإجازة.

**مثال.** الإجازة السنوية

**القواعد.** إلزامي. يُختار من قائمة أنواع الغياب.

## Year {#LeaveAccrualSummary.Year}
<!-- alias: loc:Year, dom:LeaveAccrualSummary_Year -->

**ما هو.** سنة اللقطة.

**مثال.** `2026`

**القواعد.** إلزامي. رقم صحيح.

## Month {#LeaveAccrualSummary.Month}
<!-- alias: loc:Month, dom:LeaveAccrualSummary_Month -->

**ما هو.** شهر اللقطة.

**مثال.** `4`

**القواعد.** إلزامي. رقم صحيح.

## AccrualDays {#LeaveAccrualSummary.AccrualDays}
<!-- alias: loc:AccrualDays, dom:LeaveAccrualSummary_AccrualDays -->

**ما هو.** أيام الإجازة المستحقة في الشهر.

**مثال.** `2.5`

**القواعد.** إلزامي. رقم.

## UsedDays {#LeaveAccrualSummary.UsedDays}
<!-- alias: loc:UsedDays, dom:LeaveAccrualSummary_UsedDays -->

**ما هو.** أيام الإجازة المأخوذة في الشهر.

**مثال.** `3`

**القواعد.** إلزامي. رقم.

## CumulativeAccruedDays {#LeaveAccrualSummary.CumulativeAccruedDays}
<!-- alias: loc:CumulativeAccruedDays, dom:LeaveAccrualSummary_CumulativeAccruedDays -->

**ما هو.** أيام الإجازة المستحقة حتى تاريخه.

**مثال.** `22.5`

**القواعد.** إلزامي. رقم.
