---
helpId: pay.wage-type-exception.edit
lang: ar
title: استثناءات أنواع الأجور — إنشاء / تحرير
entity: HCMv3.PayrollManagement.WageTypeException
surfaces:
  - angular/src/app/admin/payroll-management/wage-type-exceptions/create-or-edit-wage-type-exception-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.WageTypeExceptions.Create]
---

## Overview

أزواج من أنواع الأجور لا ينبغي صرفهما معاً. فعندما يصل نظام الرواتب إلى نوع الأجر يتجاوزه إذا كان لأي من أنواع الأجور المستثناة منه مبلغ أو عدد أو أجر في كشف راتب الموظف.

## WageTypes {#WageTypeException.WageTypeId}
<!-- alias: loc:WageTypes, dom:WageTypeException_WageTypeId -->

**ما هو.** نوع الأجر الذي يُتجاوز.

**مثال.** بدل المواصلات

**لماذا يهم.** يستبعده نظام الرواتب للموظف الذي يحتوي كشف راتبه على نوع الأجر المستثنى.

**القواعد.** إلزامي. يُختار من قائمة أنواع الأجور.

## ExceptWageType {#WageTypeException.ExceptWageTypeId}
<!-- alias: loc:ExceptWageType, dom:WageTypeException_ExceptWageTypeId -->

**ما هو.** نوع الأجر الذي يمنع وجودُه صرفَ الأول.

**مثال.** ميزة سيارة الشركة

**لماذا يهم.** يجب أن يكون موجوداً في كشف الراتب عند الوصول إلى نوع الأجر الأول، فاجعل ترتيبه في الرواتب أصغر.

**القواعد.** إلزامي. يُختار من قائمة أنواع الأجور.
