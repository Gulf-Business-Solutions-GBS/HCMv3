---
helpId: pa.employee-delegation.edit
lang: ar
title: تفويضات الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeDelegation
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/delegations/create-or-edit-employee-delegation-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeDelegations.Create]
---

## Overview

تسليم مهام الموظف إلى زميل خلال فترة -- عادةً أثناء إجازته.

طوال سريان التفويض، يُنسخ الزميل على رسائل الإشعارات التي يتلقاها الموظف. ويمكن توجيه التفويضات للاعتماد، ولا تسري إلا بعد اعتمادها. ولا يجوز للموظف تفويض نفسه، ولا أن تتداخل تفويضات الموظف نفسه.

## DelegateTo {#EmployeeDelegation.DelegateToId}
<!-- alias: loc:DelegateTo, dom:EmployeeDelegation_DelegateToId -->

**ما هو.** الزميل الذي يتولى مهام الموظف.

**مثال.** أحمد الكندري (1057)

**لماذا يهم.** يجب أن يكون موظفاً آخر.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.
