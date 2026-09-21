---
helpId: pay.ticket-accrual-summary.edit
lang: ar
title: ملخصات تراكم التذاكر — إنشاء / تحرير
entity: HCMv3.PayrollManagement.TicketAccrualSummary
surfaces:
  - angular/src/app/admin/payroll-management/ticket-accrual-summaries/create-or-edit-ticket-accrual-summary-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.TicketAccrualSummaries.Create]
---

## Overview

لقطة شهرية لاستحقاق تذاكر الطيران للموظف.

لا تُكتب حالياً: لا توجد عملية تملأ هذه الملخصات، ولا شيء يقرؤها. فالأرصدة تُحتسب عند الحاجة إليها من سجل الوقت وسجلات الموظف.

## EmployeeId {#TicketAccrualSummary.EmployeeId}
<!-- alias: loc:EmployeeId, dom:TicketAccrualSummary_EmployeeId -->

**ما هو.** الموظف.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## Year {#TicketAccrualSummary.Year}
<!-- alias: loc:Year, dom:TicketAccrualSummary_Year -->

**ما هو.** سنة اللقطة.

**مثال.** `2026`

**القواعد.** اختياري. رقم صحيح.

## Month {#TicketAccrualSummary.Month}
<!-- alias: loc:Month, dom:TicketAccrualSummary_Month -->

**ما هو.** شهر اللقطة.

**مثال.** `4`

**القواعد.** اختياري. رقم صحيح.

## TicketAccrued {#TicketAccrualSummary.TicketAccrued}
<!-- alias: loc:TicketAccrued, dom:TicketAccrualSummary_TicketAccrued -->

**ما هو.** التذاكر المستحقة حتى تاريخه.

**مثال.** `0.33`

**القواعد.** اختياري. رقم.

## TicketUsed {#TicketAccrualSummary.TicketUsed}
<!-- alias: loc:TicketUsed, dom:TicketAccrualSummary_TicketUsed -->

**ما هو.** التذاكر المستخدمة حتى تاريخه.

**مثال.** `0`

**القواعد.** اختياري. رقم.
