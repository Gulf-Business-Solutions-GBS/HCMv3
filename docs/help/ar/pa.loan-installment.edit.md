---
helpId: pa.loan-installment.edit
lang: ar
title: أقساط القرض — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.LoanInstallment
surfaces:
  - angular/src/app/admin/personnel-administration/loan-installments/create-or-edit-loan-installment-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.LoanInstallments.Create]
---

## Overview

قسط مخطط في جدول سداد القرض.

يُسجَّل للرجوع إليه؛ إذ تسترد الرواتب القروض بمبلغ القسط المحدد في القرض، ولا تقرأ هذا الجدول حالياً.

## EmployeeLoanId {#LoanInstallment.EmployeeLoanId}
<!-- alias: loc:EmployeeLoanId, dom:LoanInstallment_EmployeeLoanId -->

**ما هو.** القرض الذي ينتمي إليه هذا القسط.

**القواعد.** إلزامي. يُختار من قائمة.

## InstallmentDate {#LoanInstallment.InstallmentDate}
<!-- alias: loc:InstallmentDate, dom:LoanInstallment_InstallmentDate -->

**ما هو.** تاريخ استحقاق القسط.

**مثال.** 31 أكتوبر 2026

**القواعد.** إلزامي. تاريخ.

## InstallmentAmount {#LoanInstallment.InstallmentAmount}
<!-- alias: loc:InstallmentAmount, dom:LoanInstallment_InstallmentAmount -->

**ما هو.** مبلغ القسط.

**مثال.** 100.000

**القواعد.** اختياري. رقم.

## InstallmentStatus {#LoanInstallment.InstallmentStatus}
<!-- alias: loc:InstallmentStatus, dom:LoanInstallment_InstallmentStatus -->

**ما هو.** هل سُدِّد القسط.

**مثال.** غير مسدد

**القواعد.** إلزامي. إحدى القيم: غير مدفوع، مدفوع.
