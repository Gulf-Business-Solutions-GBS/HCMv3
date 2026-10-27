---
helpId: org.organization-unit-type.edit
lang: ar
title: أنواع وحدات الشركة — إنشاء / تحرير
entity: HCMv3.OrganizationManagement.OrganizationUnitType
surfaces:
  - angular/src/app/admin/organization-management/organization-unit-types/create-or-edit-organization-unit-type-modal.component.html
audience: [admin]
permissions: [Pages.OrganizationManagement.OrganizationUnitTypes.Create]
---

## Overview

مستوى في شجرة المنظمة، مثل الشركة أو القطاع أو القسم. ويجب أن يكون الرمز أحد المستويات الثابتة من Level0 إلى Level9، حيث Level0 هو الأعلى.

## AllowLeaveConflict {#OrganizationUnitType.AllowLeaveConflict}
<!-- alias: loc:AllowLeaveConflict, dom:OrganizationUnitType_AllowLeaveConflict -->

**ما هو.** ما إذا كان يجوز تداخل الإجازات داخل وحدات هذا المستوى.

**لماذا يهم.** لا يمكن تحديده إلا لمستوى واحد. لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## AssignPayrollArea {#OrganizationUnitType.AssignPayrollArea}
<!-- alias: loc:AssignPayrollArea, dom:OrganizationUnitType_AssignPayrollArea -->

**ما هو.** ما إذا كانت وحدات هذا المستوى تحمل منطقة رواتب.

**لماذا يهم.** لا يمكن تحديده إلا لمستوى واحد غير Level0. ويقرؤه إعداد الانطلاق عند إنشاء وحدة الشركة، فلا يعطي الوحدة منطقة رواتب إلا إذا كان محدداً لمستواها.

**القواعد.** نعم أو لا.
