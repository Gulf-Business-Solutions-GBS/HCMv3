---
helpId: pa.employee-cost-distribution.edit
lang: ar
title: توزيع تكاليف الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeCostDistribution
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/cost-distributions/create-or-edit-employee-cost-distribution-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeCostDistributions.Create]
---

## Overview

كيفية توزيع تكلفة الموظف على الوحدات التنظيمية خلال فترة.

يُسجَّل للرجوع إليه، ولا يقرؤه محرك التكلفة حالياً.

## OrganizationUnitId {#EmployeeCostDistribution.OrganizationUnitId}
<!-- alias: loc:OrganizationUnitId, dom:EmployeeCostDistribution_OrganizationUnitId -->

**ما هو.** الوحدة التنظيمية التي توزَّع عليها التكلفة.

**مثال.** قطاع المشاريع

**القواعد.** اختياري. يُختار من قائمة الوحدات التنظيمية.

## CostCount {#EmployeeCostDistribution.CostCount}
<!-- alias: loc:CostCount, dom:EmployeeCostDistribution_CostCount -->

**ما هو.** عدد مسجل مع التوزيع.

**مثال.** 3

**القواعد.** اختياري. رقم صحيح.

## UsedDays {#EmployeeCostDistribution.UsedDays}
<!-- alias: loc:UsedDays, dom:EmployeeCostDistribution_UsedDays -->

**ما هو.** عدد الأيام المسجلة على هذه الوحدة.

**مثال.** 12

**القواعد.** اختياري. رقم صحيح.
