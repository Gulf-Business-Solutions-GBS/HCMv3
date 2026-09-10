---
helpId: gmi.nationality.edit
lang: ar
title: الجنسيات — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.Nationality
surfaces:
  - angular/src/app/admin/general-master-info/nationalities/create-or-edit-nationality-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.Nationalities.Create]
---

## Overview

الجنسيات المستخدمة في سجلات الموظفين. تحدد جنسية الموظف قواعد التأمينات الاجتماعية التي تنطبق في الرواتب، وكيف يُحتسب في نسبة العمالة.

## NationalityGroup {#Nationality.NationalityGroupId}
<!-- alias: loc:NationalityGroup, dom:Nationality_NationalityGroupId -->

**ما هو.** المجموعة التي تنتمي إليها هذه الجنسية.

**لماذا يهم.** لأغراض نسبة العمالة ضمن إدارة العلاقات الحكومية، تُحتسب الجنسيات التي تحمل مجموعتها الاسم Citizen جنسياتِ مواطنين، وتُحتسب سائر الجنسيات غير مواطنين.

**القواعد.** اختياري. يُختار من قائمة مجموعات الجنسيات.

:::admin
تُدار المجموعات من البيانات الأساسية العامة ← مجموعات الجنسيات.
:::
