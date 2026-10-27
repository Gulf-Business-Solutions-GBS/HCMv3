---
helpId: cd.cdform.edit
lang: ar
title: نماذج تطوير الكفاءات — إنشاء / تحرير
entity: HCMv3.CompetencyDevelopment.CDForm
surfaces:
  - angular/src/app/admin/competency-development/cd-forms/create-or-edit-cd-form-modal.component.html
audience: [admin]
permissions: [Pages.CompetencyDevelopment.CDForms.Create]
---

## Overview

نموذج تطوير الكفاءات لموظف في فترة واحدة، يضم خطته ومراجعة منتصف السنة والمراجعة السنوية. ولكل موظف نموذج واحد لكل فترة.

## CDPeriod {#CDForm.CDPeriodId}
<!-- alias: loc:CDPeriod, dom:CDForm_CDPeriodId -->

**ما هو.** فترة المراجعة.

**القواعد.** إلزامي. يُختار من قائمة فترات تطوير الكفاءات.

## Employee {#CDForm.EmployeeId}
<!-- alias: loc:Employee, dom:CDForm_EmployeeId -->

**ما هو.** الموظف.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## ModeratedScore {#CDForm.ModeratedScore}
<!-- alias: loc:ModeratedScore, dom:CDForm_ModeratedScore -->

**ما هو.** الدرجة النهائية بعد المراجعة، تُدخل يدوياً.

**مثال.** 3.5

**القواعد.** اختياري. رقم.

## EmployeeSignedOff {#CDForm.EmployeeSignedOff}
<!-- alias: loc:EmployeeSignedOff, dom:CDForm_EmployeeSignedOff -->

**ما هو.** ما إذا كان الموظف قد وافق على النموذج.

**لماذا يهم.** للتسجيل فقط: يمكن تعديل النموذج بعده.

**القواعد.** نعم أو لا.

## ManagerSignedOff {#CDForm.ManagerSignedOff}
<!-- alias: loc:ManagerSignedOff, dom:CDForm_ManagerSignedOff -->

**ما هو.** ما إذا كان المدير قد وافق على النموذج.

**لماذا يهم.** للتسجيل فقط: يمكن تعديل النموذج بعده.

**القواعد.** نعم أو لا.
