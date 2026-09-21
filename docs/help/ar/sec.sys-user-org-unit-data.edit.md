---
helpId: sec.sys-user-org-unit-data.edit
lang: ar
title: بيانات وحدات الشركة للمستخدمين — إنشاء / تحرير
entity: HCMv3.SystemSecurity.SysUserOrgUnitData
surfaces:
  - angular/src/app/admin/system-security/sys-user-org-unit-datas/create-or-edit-sys-user-org-unit-data-modal.component.html
audience: [admin]
permissions: []
---

## Overview

نطاق أمان مستوى السجلات: الجزء من التنظيم الذي يستطيع مستخدمو الدور رؤيته. فيرى المستخدم الموظفين في الوحدات التنظيمية الممنوحة لأي من أدواره، وكل ما تحت تلك الوحدات. والسطر على أعلى مستوى تنظيمي يتيح للدور رؤية الجميع.

ولا يُطبَّق إلا إذا أُشِّر على *EnableRowLevelSecurity* في إعدادات الأمان.

## Employee {#SysUserOrgUnitData.EmployeeId}
<!-- alias: loc:Employee, dom:SysUserOrgUnitData_EmployeeId -->

**ما هو.** موظف يخصه النطاق.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. يُختار من قائمة الموظفين.

## Role {#SysUserOrgUnitData.RoleId}
<!-- alias: loc:Role, dom:SysUserOrgUnitData_RoleId -->

**ما هو.** الدور الذي ينطبق عليه النطاق.

**مثال.** مسؤول الموارد البشرية - الكويت

**القواعد.** إلزامي. يُختار من قائمة الأدوار.

## OrgLevel {#SysUserOrgUnitData.OrgLevelId}
<!-- alias: loc:OrgLevel, dom:SysUserOrgUnitData_OrgLevelId -->

**ما هو.** المستوى التنظيمي للوحدة.

**لماذا يهم.** يمنح المستوى الأعلى (المستوى 0) الدورَ كل الموظفين.

**القواعد.** إلزامي. يُختار من قائمة أنواع وحدات الشركة.

## OrgUnit {#SysUserOrgUnitData.OrganizationUnitId}
<!-- alias: loc:OrgUnit, dom:SysUserOrgUnitData_OrganizationUnitId -->

**ما هو.** الوحدة التنظيمية التي يستطيع الدور رؤيتها.

**مثال.** فرع الكويت

**لماذا يهم.** يرى الدور الموظفين في هذه الوحدة وفي كل وحدة تحتها.

**القواعد.** إلزامي. يُختار من قائمة الوحدات التنظيمية.

## ShowUnlocked {#SysUserOrgUnitData.ShowUnlocked}
<!-- alias: loc:ShowUnlocked, dom:SysUserOrgUnitData_ShowUnlocked -->

**ما هو.** ما إذا كانت السجلات غير المقفلة تُعرض.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.
