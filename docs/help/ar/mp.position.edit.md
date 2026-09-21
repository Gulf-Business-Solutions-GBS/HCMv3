---
helpId: mp.position.edit
lang: ar
title: الوظائف — إنشاء / تحرير
entity: HCMv3.ManpowerPlanningManagement.Position
surfaces:
  - angular/src/app/admin/manpower-planning-management/positions/create-or-edit-position-modal.component.html
audience: [admin]
permissions: [Pages.ManpowerPlanningManagement.Positions.Create]
---

## Overview

وظيفة في الهيكل: مقعد واحد أو عدة مقاعد لمسمى وظيفي. تُنشأ الوظائف يدوياً أو من خطة قوى عاملة موافق عليها، ويرسمها مخطط الوظائف من حقل "ترفع تقاريرها إلى وظيفة" لكل وظيفة. اترك الرمز فارغاً ليُعطى الرقم التالي. ولا يجوز أن تتبع الوظيفة نفسها.

## JobTitle {#Position.JobTitleId}
<!-- alias: loc:JobTitle, dom:Position_JobTitleId -->

**ما هو.** المسمى الوظيفي للوظيفة.

**مثال.** محاسب

**القواعد.** اختياري. يُختار من قائمة المسميات الوظيفية.

## OrganizationUnit {#Position.OrganizationUnitId}
<!-- alias: loc:OrganizationUnit, dom:Position_OrganizationUnitId -->

**ما هو.** القسم الذي تقع فيه الوظيفة.

**القواعد.** اختياري. يُختار من قائمة Abp.Organizations.OrganizationUnit.

## ParentPosition {#Position.ParentPositionId}
<!-- alias: loc:ParentPosition, dom:Position_ParentPositionId -->

**ما هو.** الوظيفة التي تتبعها هذه الوظيفة.

**مثال.** مدير المالية

**لماذا يهم.** يبني مخطط الوظائف.

**القواعد.** اختياري. يُختار من قائمة الوظائف.

## CostCenter {#Position.CostCenterId}
<!-- alias: loc:CostCenter, dom:Position_CostCenterId -->

**ما هو.** مركز تكلفة الوظيفة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. يُختار من قائمة مراكز التكلفة.

## EmployeeGroup {#Position.EmployeeGroupId}
<!-- alias: loc:EmployeeGroup, dom:Position_EmployeeGroupId -->

**ما هو.** مجموعة الموظفين للوظيفة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. يُختار من قائمة.

## EmployeeSubGroup {#Position.EmployeeSubGroupId}
<!-- alias: loc:EmployeeSubGroup, dom:Position_EmployeeSubGroupId -->

**ما هو.** المجموعة الفرعية للموظفين للوظيفة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. يُختار من قائمة.

## PayrollAreaId {#Position.PayrollAreaId}
<!-- alias: loc:PayrollAreaId, dom:Position_PayrollAreaId -->

**ما هو.** منطقة الرواتب للوظيفة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. يُختار من قائمة مناطق الرواتب.

## WorkLocationId {#Position.WorkLocationId}
<!-- alias: loc:WorkLocationId, dom:Position_WorkLocationId -->

**ما هو.** موقع العمل للوظيفة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. يُختار من قائمة أماكن العمل.

## FTELimit {#Position.FTELimit}
<!-- alias: loc:FTELimit, dom:Position_FTELimit -->

**ما هو.** عدد الأشخاص الذين يمكنهم شغل الوظيفة في الوقت نفسه.

**مثال.** 1

**لماذا يهم.** يجب أن يكون أكبر من 0. ويُرفض إضافة شاغل تجعل تواريخه الوظيفة تتجاوز هذا العدد.

**القواعد.** إلزامي. رقم صحيح.

## MinSalary {#Position.MinSalary}
<!-- alias: loc:MinSalary, dom:Position_MinSalary -->

**ما هو.** أدنى راتب للوظيفة.

**مثال.** 700.000

**لماذا يهم.** يجب أن تبقى أسطر خطة القوى العاملة لهذه الوظيفة ضمن الحد الأدنى والأقصى.

**القواعد.** إلزامي. رقم.

## MidSalary {#Position.MidSalary}
<!-- alias: loc:MidSalary, dom:Position_MidSalary -->

**ما هو.** الراتب المتوسط للوظيفة.

**مثال.** 800.000

**القواعد.** إلزامي. رقم.

## MaxSalary {#Position.MaxSalary}
<!-- alias: loc:MaxSalary, dom:Position_MaxSalary -->

**ما هو.** أعلى راتب للوظيفة.

**مثال.** 1,000.000

**لماذا يهم.** انظر الحد الأدنى للراتب.

**القواعد.** إلزامي. رقم.

## Vacant {#Position.Vacant}
<!-- alias: loc:Vacant, dom:Position_Vacant -->

**ما هو.** ما إذا كانت الوظيفة غير مشغولة اليوم.

**لماذا يهم.** يضبطه النظام كلما أُضيف شاغل أو عُدّل أو أُزيل.

**القواعد.** نعم أو لا.

## Active {#Position.Active}
<!-- alias: loc:Active, dom:Position_Active -->

**ما هو.** ما إذا كانت الوظيفة مستخدمة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## IsBudgeted {#Position.IsBudgeted}
<!-- alias: loc:IsBudgeted, dom:Position_IsBudgeted -->

**ما هو.** ما إذا كانت الوظيفة ضمن الميزانية.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.
