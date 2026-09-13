---
helpId: pa.employee-benefit-exception.edit
lang: ar
title: استثناءات مزايا الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeBenefitException
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/benefit-exceptions/create-or-edit-employee-benefit-exception-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeBenefitExceptions.Create]
---

## Overview

يستبدل استحقاق الموظف نفسه من التذاكر أو تعويض المصاريف الصحية خلال فترة.

في تعويض المصاريف الصحية، يحل المبلغ محل حصة الموظف من استحقاق مجموعته الفرعية. وفي التذاكر، يستخدم محرك الوقت المبلغ عند احتساب تراكم تذكرة الموظف نفسه؛ ويعني المبلغ صفر عدم الاستحقاق.

## ExceptionBenefitType {#EmployeeBenefitException.ExceptionBenefitType}
<!-- alias: loc:ExceptionBenefitType, dom:EmployeeBenefitException_ExceptionBenefitType -->

**ما هو.** الميزة التي يُستبدل استحقاقها.

**مثال.** تعويض المصاريف الصحية

**القواعد.** إلزامي. إحدى القيم: التذكرة، استرداد المصاريف الصحية.

## Amount {#EmployeeBenefitException.Amount}
<!-- alias: loc:Amount, dom:EmployeeBenefitException_Amount -->

**ما هو.** مبلغ الاستحقاق الذي يُطبَّق بدلاً منه.

**مثال.** 300.000

**لماذا يهم.** في التذاكر، يعني الصفر أن الموظف غير مستحق خلال الفترة.

**القواعد.** اختياري. رقم.
