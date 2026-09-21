---
helpId: suc.successor-nominee.edit
lang: ar
title: المرشحون للتعاقب الوظيفي — إنشاء / تحرير
entity: HCMv3.SuccessionManagement.SuccessorNominee
surfaces:
  - angular/src/app/admin/succession-management/successor-nominees/create-or-edit-successor-nominee-modal.component.html
audience: [admin]
permissions: [Pages.SuccessionManagement.SuccessorNominees.Create]
---

## Overview

موظف مرشَّح لخلافة موظف آخر، مع مدى جاهزيته. ولا يُرشَّح الشخص نفسه إلا مرة واحدة لكل موظف. يُحفظ كسجل: لا يقرؤه شيء حالياً.

## Employee {#SuccessorNominee.EmployeeId}
<!-- alias: loc:Employee, dom:SuccessorNominee_EmployeeId -->

**ما هو.** الموظف الذي تُراد خلافته.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## Nominee {#SuccessorNominee.NomineeId}
<!-- alias: loc:Nominee, dom:SuccessorNominee_NomineeId -->

**ما هو.** الموظف المرشَّح للخلافة.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## ReadinessId {#SuccessorNominee.ReadinessId}
<!-- alias: loc:ReadinessId, dom:SuccessorNominee_ReadinessId -->

**ما هو.** مدى سرعة جاهزية المرشَّح لتولي المنصب.

**مثال.** جاهز خلال سنة إلى سنتين

**القواعد.** إلزامي. يُختار من قائمة درجات الجاهزية.

## RankingId {#SuccessorNominee.RankingId}
<!-- alias: loc:RankingId, dom:SuccessorNominee_RankingId -->

**ما هو.** ترتيب المرشَّح بين الخلفاء.

**مثال.** 1

**القواعد.** إلزامي. يُختار من قائمة الترتيب.
