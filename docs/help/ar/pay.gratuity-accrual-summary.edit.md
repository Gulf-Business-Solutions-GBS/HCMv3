---
helpId: pay.gratuity-accrual-summary.edit
lang: ar
title: ملخصات تراكم مكافأة نهاية الخدمة — إنشاء / تحرير
entity: HCMv3.PayrollManagement.GratuityAccrualSummary
surfaces:
  - angular/src/app/admin/payroll-management/gratuity-accrual-summaries/create-or-edit-gratuity-accrual-summary-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.GratuityAccrualSummaries.Create]
---

## Overview

لقطة شهرية لمكافأة نهاية الخدمة للموظف: الأيام المستحقة والرصيد والمبالغ.

لا تُكتب حالياً: لا توجد عملية تملأ هذه الملخصات، ولا شيء يقرؤها. فالأرصدة تُحتسب عند الحاجة إليها من سجل الوقت وسجلات الموظف.

## EmployeeId {#GratuityAccrualSummary.EmployeeId}
<!-- alias: loc:EmployeeId, dom:GratuityAccrualSummary_EmployeeId -->

**ما هو.** الموظف.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## Year {#GratuityAccrualSummary.Year}
<!-- alias: loc:Year, dom:GratuityAccrualSummary_Year -->

**ما هو.** سنة اللقطة.

**مثال.** `2026`

**القواعد.** إلزامي. رقم صحيح.

## Month {#GratuityAccrualSummary.Month}
<!-- alias: loc:Month, dom:GratuityAccrualSummary_Month -->

**ما هو.** شهر اللقطة.

**مثال.** `4`

**القواعد.** إلزامي. رقم صحيح.

## GratuityDaysAccrued {#GratuityAccrualSummary.GratuityDaysAccrued}
<!-- alias: loc:GratuityDaysAccrued, dom:GratuityAccrualSummary_GratuityDaysAccrued -->

**ما هو.** أيام المكافأة المستحقة في الشهر.

**مثال.** `1.25`

**القواعد.** إلزامي. رقم.

## GratuityDaysBalance {#GratuityAccrualSummary.GratuityDaysBalance}
<!-- alias: loc:GratuityDaysBalance, dom:GratuityAccrualSummary_GratuityDaysBalance -->

**ما هو.** أيام المكافأة المستحقة حتى تاريخه.

**مثال.** `37.5`

**القواعد.** إلزامي. رقم.

## GratuityAmount {#GratuityAccrualSummary.GratuityAmount}
<!-- alias: loc:GratuityAmount, dom:GratuityAccrualSummary_GratuityAmount -->

**ما هو.** قيمة المكافأة حتى تاريخه.

**مثال.** `1000.000`

**القواعد.** إلزامي. رقم.

## GratuityAmountPaid {#GratuityAccrualSummary.GratuityAmountPaid}
<!-- alias: loc:GratuityAmountPaid, dom:GratuityAccrualSummary_GratuityAmountPaid -->

**ما هو.** ما صُرف من المكافأة.

**مثال.** `0.000`

**القواعد.** إلزامي. رقم.
