---
helpId: prj.project-wage-type.edit
lang: ar
title: أنواع أجور المشروع — إنشاء / تحرير
entity: HCMv3.ProjectManagement.ProjectWageType
surfaces:
  - angular/src/app/admin/project-management/project-wage-types/create-or-edit-project-wage-type-modal.component.html
audience: [admin]
permissions: [Pages.ProjectManagement.ProjectWageTypes.Create]
---

## Overview

نوع أجر يُحمَّل على مشروع. لا تُوزَّع على المشروع إلا بنود الراتب التي يكون نوع أجرها مدرجاً هنا، ويُستبعد كل ما سواها في سجل الرواتب. ولا يُدرج نوع الأجر إلا مرة واحدة لكل مشروع.

## Project {#ProjectWageType.ProjectId}
<!-- alias: loc:Project, dom:ProjectWageType_ProjectId -->

**ما هو.** المشروع.

**القواعد.** إلزامي. يُختار من قائمة المشاريع.

## WageType {#ProjectWageType.WageTypeId}
<!-- alias: loc:WageType, dom:ProjectWageType_WageTypeId -->

**ما هو.** نوع الأجر.

**مثال.** الراتب الأساسي

**القواعد.** إلزامي. يُختار من قائمة أنواع الأجور.
