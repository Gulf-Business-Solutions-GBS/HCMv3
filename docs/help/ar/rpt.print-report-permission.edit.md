---
helpId: rpt.print-report-permission.edit
lang: ar
title: صلاحيات طباعة التقارير — إنشاء / تحرير
entity: HCMv3.ReportManagement.PrintReportPermission
surfaces:
  - angular/src/app/admin/report-management/print-report-permissions/create-or-edit-print-report-permission-modal.component.html
audience: [admin]
permissions: [Pages.ReportManagement.PrintReportPermissions.Create]
---

## Overview

تقارير الطباعة التي يجوز لمستخدمي دور واحد تشغيلها. يرى المستخدم التقارير الممنوحة لأي من أدواره. والمستخدم الذي ليست له منح لا يرى أياً منها، لكن مسؤولي الشركة ومستخدمي المضيف يرون دائماً كل التقارير النشطة. ولكل دور قيد واحد.

## Role {#PrintReportPermission.RoleId}
<!-- alias: loc:Role, dom:PrintReportPermission_RoleId -->

**ما هو.** الدور الذي تُمنح له التقارير.

**مثال.** الرواتب

**القواعد.** إلزامي. يُختار من قائمة الأدوار.
