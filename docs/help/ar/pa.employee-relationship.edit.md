---
helpId: pa.employee-relationship.edit
lang: ar
title: صلات قرابة الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeRelationship
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/relationships/create-or-edit-employee-relationship-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeRelationships.Create]
---

## Overview

صلة بين هذا الموظف وموظف آخر -- كزوج أو قريب يعمل في المؤسسة نفسها.

تُسجَّل للرجوع إليها، ولا يقرؤها أي احتساب أو خطوة اعتماد حالياً.

## RelationshipType {#EmployeeRelationship.RelationshipTypeId}
<!-- alias: loc:RelationshipType, dom:EmployeeRelationship_RelationshipTypeId -->

**ما هو.** صلة القرابة بين الموظفَين.

**مثال.** زوج

**القواعد.** إلزامي. يُختار من قائمة صلات القرابة.

:::admin
تُدار من البيانات الأساسية العامة ← صلات القرابة.
:::

## RelatedEmployee {#EmployeeRelationship.RelatedEmployeeId}
<!-- alias: loc:RelatedEmployee, dom:EmployeeRelationship_RelatedEmployeeId -->

**ما هو.** الموظف الآخر.

**مثال.** سارة المطيري (1188)

**القواعد.** إلزامي. يُختار من قائمة الموظفين.
