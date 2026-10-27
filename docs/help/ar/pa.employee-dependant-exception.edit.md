---
helpId: pa.employee-dependant-exception.edit
lang: ar
title: استثناءات عائلي الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeDependantException
surfaces:
  - angular/src/app/admin/personnel-administration/employee-dependant-exceptions/create-or-edit-employee-dependant-exception-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeDependantExceptions.Create]
---

## Overview

يستبدل استحقاق أحد المعالين من التذاكر أو تعويض المصاريف الصحية خلال فترة. ويجب أن تقع الفترة ضمن تواريخ المعال نفسه.

في تعويض المصاريف الصحية، يحل المبلغ محل حصة ذلك المعال. وفي التذاكر، يستخدمه محرك الوقت عند احتساب تراكم تذكرة المعال.

## EmployeeDependantId {#EmployeeDependantException.EmployeeDependantId}
<!-- alias: loc:EmployeeDependantId, dom:EmployeeDependantException_EmployeeDependantId -->

**ما هو.** المعال الذي ينطبق عليه الاستثناء.

**مثال.** سارة المطيري (ابنة)

**القواعد.** إلزامي. يُختار من قائمة.

## ExceptionBenefitType {#EmployeeDependantException.ExceptionBenefitType}
<!-- alias: loc:ExceptionBenefitType, dom:EmployeeDependantException_ExceptionBenefitType -->

**ما هو.** الميزة التي يُستبدل استحقاقها.

**مثال.** تذكرة

**القواعد.** إلزامي. إحدى القيم: التذكرة، استرداد المصاريف الصحية.

## Amount {#EmployeeDependantException.Amount}
<!-- alias: loc:Amount, dom:EmployeeDependantException_Amount -->

**ما هو.** مبلغ الاستحقاق الذي يُطبَّق بدلاً منه.

**مثال.** 150.000

**القواعد.** اختياري. رقم.
