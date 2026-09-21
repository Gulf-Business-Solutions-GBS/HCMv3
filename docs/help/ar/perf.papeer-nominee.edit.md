---
helpId: perf.papeer-nominee.edit
lang: ar
title: المرشحون من الزملاء للتقييم — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.PAPeerNominee
surfaces:
  - angular/src/app/admin/performance-management/pa-peer-nominees/create-or-edit-pa-peer-nominee-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.PAPeerNominees.Create]
---

## Overview

زميل رشّحه الموظف لمراجعته في فترة. ولا يُرشَّح الزميل إلا مرة واحدة لكل موظف وفترة. وتظهر مراجعة الزميل مقدَّمة عندما يُؤشَّر على ترشيحه بأنه مقدَّم.

## PAPeriod {#PAPeerNominee.PAPeriodId}
<!-- alias: loc:PAPeriod, dom:PAPeerNominee_PAPeriodId -->

**ما هو.** فترة التقييم.

**القواعد.** إلزامي. يُختار من قائمة فترات تقييم الأداء.

## Employee {#PAPeerNominee.EmployeeId}
<!-- alias: loc:Employee, dom:PAPeerNominee_EmployeeId -->

**ما هو.** الموظف الجاري مراجعته.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## Peer {#PAPeerNominee.PeerId}
<!-- alias: loc:Peer, dom:PAPeerNominee_PeerId -->

**ما هو.** الزميل المرشَّح.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## Submitted {#PAPeerNominee.Submitted}
<!-- alias: loc:Submitted, dom:PAPeerNominee_Submitted -->

**ما هو.** ما إذا كان الزميل قد قدّم المراجعة.

**القواعد.** نعم أو لا.
