---
helpId: pa.employee-sponsor.edit
lang: ar
title: كفلاء الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeSponsor
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/sponsors/create-or-edit-employee-sponsor-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeSponsors.Create]
---

## Overview

الكفيل الذي تقع تأشيرة عمل الموظف تحت كفالته خلال فترة، مع المسمى الوظيفي الحكومي وراتب العقد.

عند إنشاء التخصيص يُحتسب ضمن حصة الكفيل لذلك المسمى الوظيفي الحكومي، مواطناً أو غير مواطن بحسب جنسية الموظف؛ وعند حذفه يُفرج عن المكان. ولا يؤدي تعديل تخصيص قائم إلى نقل العدد. ويُرفض التخصيص إذا لم تكن للكفيل حصة لذلك المسمى الوظيفي الحكومي.

## SponsorId {#EmployeeSponsor.SponsorId}
<!-- alias: loc:SponsorId, dom:EmployeeSponsor_SponsorId -->

**ما هو.** الكفيل.

**مثال.** شركة المطيري للتجارة العامة

**لماذا يهم.** يمكن تصفية ملفات البنك وتوزيع التقارير حسب الكفيل.

**القواعد.** إلزامي. يُختار من قائمة الكفلاء.

:::admin
يُدار من إدارة العلاقات الحكومية ← الكفلاء.
:::

## GovtJobTitleId {#EmployeeSponsor.GovtJobTitleId}
<!-- alias: loc:GovtJobTitleId, dom:EmployeeSponsor_GovtJobTitleId -->

**ما هو.** المسمى الوظيفي المسجل لدى الجهات الحكومية.

**مثال.** محاسب

**لماذا يهم.** يجب أن تكون للكفيل حصة لهذا المسمى؛ ويشغل التخصيص أحد أماكنها.

**القواعد.** إلزامي. يُختار من قائمة المسميات الوظيفية الحكومية.

## EmpSponsorRelation {#EmployeeSponsor.EmpSponsorRelation}
<!-- alias: loc:EmpSponsorRelation, dom:EmployeeSponsor_EmpSponsorRelation -->

**ما هو.** صلة الموظف بالكفيل، كنص حر.

**مثال.** موظف

**لماذا يهم.** يظهر في تقرير الكفلاء.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## ContractSalary {#EmployeeSponsor.ContractSalary}
<!-- alias: loc:ContractSalary, dom:EmployeeSponsor_ContractSalary -->

**ما هو.** الراتب في العقد الحكومي.

**مثال.** 600.000

**لماذا يهم.** عند ضبط التأمينات الاجتماعية على استخدام راتب العقد، تأخذه الرواتب من تخصيص الكفيل الذي يغطي الشهر.

**القواعد.** اختياري. رقم.

## SalaryCurrencyId {#EmployeeSponsor.SalaryCurrencyId}
<!-- alias: loc:SalaryCurrencyId, dom:EmployeeSponsor_SalaryCurrencyId -->

**ما هو.** عملة راتب العقد.

**مثال.** KWD

**لماذا يهم.** يُسجَّل للرجوع إليه، ولا يقرؤه أي احتساب أو تقرير حالياً.

**القواعد.** إلزامي. يُختار من قائمة العملات.

## ResidencyType {#EmployeeSponsor.ResidencyType}
<!-- alias: loc:ResidencyType, dom:EmployeeSponsor_ResidencyType -->

**ما هو.** هل الإقامة على الشركة أم لا.

**مثال.** الشركة

**لماذا يهم.** يظهر في تقارير الموظفين.

**القواعد.** إلزامي. إحدى القيم: الشركة، خارج الشركة.

## WorkVisaTypeId {#EmployeeSponsor.WorkVisaTypeId}
<!-- alias: loc:WorkVisaTypeId, dom:EmployeeSponsor_WorkVisaTypeId -->

**ما هو.** نوع تأشيرة العمل.

**مثال.** مادة 18

**لماذا يهم.** يُسجَّل للرجوع إليه، ولا يقرؤه أي احتساب أو تقرير حالياً.

**القواعد.** إلزامي. يُختار من قائمة أنواع تأشيرات العمل.
