---
helpId: mp.manpower-planning.edit
lang: ar
title: خطط القوى العاملة — إنشاء / تحرير
entity: HCMv3.ManpowerPlanningManagement.ManpowerPlanning
surfaces:
  - angular/src/app/admin/manpower-planning-management/manpower-plannings/create-or-edit-manpower-planning-modal.component.html
audience: [admin]
permissions: [Pages.ManpowerPlanningManagement.ManpowerPlannings.Create]
---

## Overview

عدد الموظفين الذي يطلبه قسم خلال فترة تخطيط: سطر لكل مسمى وظيفي مع ميزانية شهرية. ولكل قسم خطة واحدة لكل فترة. تذهب الخطة إلى الموافقة، التي تتطلب فترة وسطراً واحداً على الأقل، ويرى المعتمد القسم والفترة والمبلغ. وعند الموافقة عليها، تُنشأ وظيفة شاغرة ومعتمدة في الميزانية لكل سطر ليست له وظيفة بعد.

## Employee {#ManpowerPlanning.EmployeeId}
<!-- alias: loc:Employee, dom:ManpowerPlanning_EmployeeId -->

**ما هو.** الموظف الذي يقدم الخطة.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## OrganizationUnit {#ManpowerPlanning.OrganizationUnitId}
<!-- alias: loc:OrganizationUnit, dom:ManpowerPlanning_OrganizationUnitId -->

**ما هو.** القسم الذي تخصه الخطة.

**مثال.** المالية

**القواعد.** إلزامي. يُختار من قائمة Abp.Organizations.OrganizationUnit.

## Period {#ManpowerPlanning.PeriodId}
<!-- alias: loc:Period, dom:ManpowerPlanning_PeriodId -->

**ما هو.** فترة التخطيط.

**القواعد.** إلزامي. يُختار من قائمة فترات تخطيط القوى العاملة.

## RequestComments {#ManpowerPlanning.RequestComments}
<!-- alias: loc:RequestComments, dom:ManpowerPlanning_RequestComments -->

**ما هو.** ملاحظات للمعتمد.

**القواعد.** اختياري. الحد الأقصى 1000 حرف.
