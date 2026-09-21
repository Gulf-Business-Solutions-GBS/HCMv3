---
helpId: prj.project-register-detail.edit
lang: ar
title: تفاصيل سجلات المشاريع — إنشاء / تحرير
entity: HCMv3.ProjectManagement.ProjectRegisterDetail
surfaces:
  - angular/src/app/admin/project-management/project-register-details/create-or-edit-project-register-detail-modal.component.html
audience: [admin]
permissions: [Pages.ProjectManagement.ProjectRegisterDetails.Create]
---

## Overview

تكلفة نوع أجر واحد المحمّلة على مشروع واحد. يكتبه محرك تكلفة المشاريع، ولا يوجد عادةً سبب لإضافة قيود أو تعديلها يدوياً.

## ProjectRegister {#ProjectRegisterDetail.ProjectRegisterId}
<!-- alias: loc:ProjectRegister, dom:ProjectRegisterDetail_ProjectRegisterId -->

**ما هو.** سجل مشاريع الموظف للفترة.

**القواعد.** إلزامي. يُختار من قائمة سجلات المشاريع.

## Project {#ProjectRegisterDetail.ProjectId}
<!-- alias: loc:Project, dom:ProjectRegisterDetail_ProjectId -->

**ما هو.** المشروع.

**القواعد.** إلزامي. يُختار من قائمة المشاريع.

## WageType {#ProjectRegisterDetail.WageTypeId}
<!-- alias: loc:WageType, dom:ProjectRegisterDetail_WageTypeId -->

**ما هو.** نوع الأجر.

**القواعد.** إلزامي. يُختار من قائمة أنواع الأجور.

## Number {#ProjectRegisterDetail.Number}
<!-- alias: loc:Number, dom:ProjectRegisterDetail_Number -->

**ما هو.** الحصة من كمية بند الراتب، مثل الأيام.

**مثال.** 12

**القواعد.** إلزامي. رقم.

## Amount {#ProjectRegisterDetail.Amount}
<!-- alias: loc:Amount, dom:ProjectRegisterDetail_Amount -->

**ما هو.** الحصة من مبلغ بند الراتب.

**مثال.** 450.000

**القواعد.** إلزامي. رقم.
