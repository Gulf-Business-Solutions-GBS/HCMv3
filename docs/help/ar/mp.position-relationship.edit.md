---
helpId: mp.position-relationship.edit
lang: ar
title: علاقات الوظائف — إنشاء / تحرير
entity: HCMv3.ManpowerPlanningManagement.PositionRelationship
surfaces:
  - angular/src/app/admin/manpower-planning-management/position-relationships/create-or-edit-position-relationship-modal.component.html
audience: [admin]
permissions: [Pages.ManpowerPlanningManagement.PositionRelationships.Create]
---

## Overview

موظف يشغل وظيفة لفترة من التواريخ. لا يمكن أن يكون تاريخ الانتهاء قبل تاريخ البدء، ولا يجوز تجاوز "حد الوظائف بدوام كامل" للوظيفة. ويُحدَّث مؤشر "شاغرة" للوظيفة، ويظهر الشاغل في مخطط الوظائف.

## Position {#PositionRelationship.PositionId}
<!-- alias: loc:Position, dom:PositionRelationship_PositionId -->

**ما هو.** الوظيفة.

**القواعد.** إلزامي. يُختار من قائمة الوظائف.

## RelationType {#PositionRelationship.RelationType}
<!-- alias: loc:RelationType, dom:PositionRelationship_RelationType -->

**ما هو.** علاقة الموظف بالوظيفة. "شاغل الوظيفة" هو الخيار الوحيد.

**القواعد.** إلزامي. إحدى القيم: شاغل الوظيفة.

## Employee {#PositionRelationship.EmployeeId}
<!-- alias: loc:Employee, dom:PositionRelationship_EmployeeId -->

**ما هو.** الموظف الذي يشغل الوظيفة.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.
