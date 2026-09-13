---
helpId: pa.employee-health-reimburse.edit
lang: ar
title: استردادات المصاريف الصحية للموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeHealthReimburse
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/health-reimburses/create-or-edit-employee-health-reimburse-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeHealthReimburses.Create]
---

## Overview

طلب تعويض مصاريف صحية للموظف. وتصرف الرواتب الطلبات التي يقع تاريخ دفعها في الشهر الذي تعالجه.

يُتحقق عند كل حفظ من الطلب مقابل استحقاق الموظف في التاريخ، سواء أُدخل هنا أو طُلب عبر الخدمة الذاتية: فيُرفض الطلب إذا لم يكن للموظف استحقاق، أو كان المبلغ صفراً، أو وُجد طلب آخر في التاريخ نفسه. وعند توجيه نوع الطلب للاعتماد، ينتظر الطلب المُدخل هنا الاعتماد أيضاً.

## PaymentDate {#EmployeeHealthReimburse.PaymentDate}
<!-- alias: loc:PaymentDate, dom:EmployeeHealthReimburse_PaymentDate -->

**ما هو.** تاريخ دفع المصروف.

**مثال.** 5 سبتمبر 2026

**لماذا يهم.** طلب واحد لكل تاريخ. وتصرف الرواتب الطلب في شهر هذا التاريخ.

**القواعد.** إلزامي. تاريخ.

## Amount {#EmployeeHealthReimburse.Amount}
<!-- alias: loc:Amount, dom:EmployeeHealthReimburse_Amount -->

**ما هو.** المبلغ المطلوب.

**مثال.** 45.500

**لماذا يهم.** لا يجوز أن يكون صفراً، ويُتحقق منه مقابل الاستحقاق المتبقي للموظف.

**القواعد.** إلزامي. رقم.
