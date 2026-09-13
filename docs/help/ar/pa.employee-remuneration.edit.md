---
helpId: pa.employee-remuneration.edit
lang: ar
title: مكافآت الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeRemuneration
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/remunerations/create-or-edit-employee-remuneration-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeRemunerations.Create]
---

## Overview

أيام أو ساعات يُدفع أجرها للموظف في تاريخ معين، للموظفين الذين يُدفع لهم باليوم أو بالساعة.

عندما تكون دورية الأجر الأساسي للموظف يومية أو بالساعة، تجمع الرواتب الأيام أو الساعات المسجلة هنا خلال الشهر وتصرفها بمعدل الأجر الأساسي.

## WageTypeId {#EmployeeRemuneration.WageTypeId}
<!-- alias: loc:WageTypeId, dom:EmployeeRemuneration_WageTypeId -->

**ما هو.** نوع أجر يُسجَّل مع القيد.

**مثال.** الراتب الأساسي

**لماذا يهم.** يحتسب الاحتساب اليومي وبالساعة كل قيود الشهر أياً كان نوع الأجر فيها.

**القواعد.** اختياري. يُختار من قائمة أنواع الأجور.

## PaymentDate {#EmployeeRemuneration.PaymentDate}
<!-- alias: loc:PaymentDate, dom:EmployeeRemuneration_PaymentDate -->

**ما هو.** التاريخ الذي تنتمي إليه الأيام أو الساعات.

**مثال.** 15 سبتمبر 2026

**لماذا يهم.** تُحتسب القيود المؤرخة في شهر الرواتب.

**القواعد.** إلزامي. تاريخ.

## PaymentNumber {#EmployeeRemuneration.PaymentNumber}
<!-- alias: loc:PaymentNumber, dom:EmployeeRemuneration_PaymentNumber -->

**ما هو.** عدد الأيام أو الساعات.

**مثال.** 22

**لماذا يهم.** لا يجوز أن يكون صفراً. وتُحوَّل الساعات إلى أيام بحسب ساعات عمل الموظف في اليوم.

**القواعد.** إلزامي. رقم.

## PaymentUnit {#EmployeeRemuneration.PaymentUnit}
<!-- alias: loc:PaymentUnit, dom:EmployeeRemuneration_PaymentUnit -->

**ما هو.** هل العدد أيام أم ساعات.

**مثال.** أيام

**القواعد.** إلزامي. إحدى القيم: أيام، ساعات.
