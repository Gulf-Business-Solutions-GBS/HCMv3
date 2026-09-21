---
helpId: pay.wage-type-security-role.edit
lang: ar
title: أدوار صلاحيات أنواع الأجور — إنشاء / تحرير
entity: HCMv3.PayrollManagement.WageTypeSecurityRole
surfaces:
  - angular/src/app/admin/payroll-management/wage-type-security-roles/create-or-edit-wage-type-security-role-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.WageTypeSecurityRoles.Create]
---

## Overview

الأدوار المسموح لها برؤية نوع أجر محمي. لا يُطبَّق حالياً: لا شيء يقيّد الوصول إلى نوع الأجر مهما أُدرج هنا.

## WageTypeId {#WageTypeSecurityRole.WageTypeId}
<!-- alias: loc:WageTypeId, dom:WageTypeSecurityRole_WageTypeId -->

**ما هو.** نوع الأجر المحمي.

**مثال.** مكافأة الإدارة التنفيذية

**القواعد.** إلزامي. يُختار من قائمة أنواع الأجور.

## RoleId {#WageTypeSecurityRole.RoleId}
<!-- alias: loc:RoleId, dom:WageTypeSecurityRole_RoleId -->

**ما هو.** دور مسموح له برؤيته.

**مثال.** مدير الرواتب

**لماذا يهم.** لا يُدرج الدور إلا مرة واحدة لكل نوع أجر.

**القواعد.** إلزامي. يُختار من قائمة الأدوار.
