---
helpId: pa.employee-extd-work-location.edit
lang: ar
title: أماكن العمل الإضافية للموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeExtdWorkLocation
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/extd-work-locations/create-or-edit-employee-extd-work-location-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeExtdWorkLocations.Create]
---

## Overview

مكان عمل إضافي يمكن للموظف العمل فيه خلال فترة، إلى جانب مكان العمل في تعيينه التنظيمي. ولا يجوز تسجيل المكان نفسه لفترات متداخلة.

يُسجَّل للرجوع إليه، ولا يقرؤه أي احتساب أو خطوة اعتماد حالياً.

## WorkLocationId {#EmployeeExtdWorkLocation.WorkLocationId}
<!-- alias: loc:WorkLocationId, dom:EmployeeExtdWorkLocation_WorkLocationId -->

**ما هو.** مكان العمل الإضافي.

**مثال.** المستودع — الشويخ

**القواعد.** إلزامي. يُختار من قائمة أماكن العمل.

:::admin
يُدار من إدارة الهيكل التنظيمي ← أماكن العمل.
:::
