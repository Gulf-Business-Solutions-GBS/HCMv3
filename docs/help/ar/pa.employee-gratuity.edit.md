---
helpId: pa.employee-gratuity.edit
lang: ar
title: مكافأة نهاية الخدمة للموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeGratuity
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/gratuities/create-or-edit-employee-gratuity-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeGratuities.Create]
---

## Overview

دفعة مكافأة نهاية خدمة صُرفت للموظف بالفعل، بالأيام.

تحتسب الرواتب الأيام المصروفة في الشهر عند احتساب مكافأة نهاية الخدمة المستحقة. وتحديد خيار إعادة الضبط يجعل احتساب المكافأة يبدأ من جديد اعتباراً من تاريخ الصرف.

## GratuityPaidDate {#EmployeeGratuity.GratuityPaidDate}
<!-- alias: loc:GratuityPaidDate, dom:EmployeeGratuity_GratuityPaidDate -->

**ما هو.** تاريخ صرف مكافأة نهاية الخدمة.

**مثال.** 31 ديسمبر 2025

**لماذا يهم.** تُحتسب الأيام المصروفة في شهر الرواتب لهذا التاريخ.

**القواعد.** إلزامي. تاريخ.

## GratuityPaidDays {#EmployeeGratuity.GratuityPaidDays}
<!-- alias: loc:GratuityPaidDays, dom:EmployeeGratuity_GratuityPaidDays -->

**ما هو.** عدد أيام مكافأة نهاية الخدمة المصروفة.

**مثال.** 120

**القواعد.** اختياري. رقم.

## ResetGratuityCalc {#EmployeeGratuity.ResetGratuityCalc}
<!-- alias: loc:ResetGratuityCalc, dom:EmployeeGratuity_ResetGratuityCalc -->

**ما هو.** هل يُعاد بدء احتساب مكافأة نهاية الخدمة من تاريخ الصرف.

**لماذا يهم.** عند تحديده، تُحتسب مدة الخدمة لأغراض المكافأة من تاريخ هذه الدفعة بدلاً من البداية.

**القواعد.** نعم أو لا.
