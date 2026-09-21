---
helpId: pay.org-unit-wage-acc-group.edit
lang: ar
title: مجموعات حسابات الأجور لوحدات الشركة — إنشاء / تحرير
entity: HCMv3.PayrollManagement.OrgUnitWageAccGroup
surfaces:
  - angular/src/app/admin/payroll-management/org-unit-wage-acc-groups/create-or-edit-org-unit-wage-acc-group-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.OrgUnitWageAccGroups.Create]
---

## Overview

مجموعة حسابات الأجور التي تستخدمها وحدة تنظيمية، اعتباراً من تاريخ معيّن.

لا يُقرأ حالياً: يأخذ محرك التكلفة وقيد اليومية الحسابات من حسابات الأجور الخاصة بكل موظف، ولا يقرأ شيءٌ هذا التعيين. وهو محفوظ للرجوع إليه ولاستخدام مستقبلي.

## OrganizationUnitId {#OrgUnitWageAccGroup.OrganizationUnitId}
<!-- alias: loc:OrganizationUnitId, dom:OrgUnitWageAccGroup_OrganizationUnitId -->

**ما هو.** الوحدة التنظيمية.

**مثال.** إدارة المالية

**القواعد.** إلزامي. يُختار من قائمة الوحدات التنظيمية.

## WageAccGroupId {#OrgUnitWageAccGroup.WageAccGroupId}
<!-- alias: loc:WageAccGroupId, dom:OrgUnitWageAccGroup_WageAccGroupId -->

**ما هو.** مجموعة حسابات الأجور التي تستخدمها الوحدة اعتباراً من تاريخ البداية.

**مثال.** حسابات المكتب الرئيسي

**القواعد.** إلزامي. يُختار من قائمة مجموعات حسابات الأجور.
