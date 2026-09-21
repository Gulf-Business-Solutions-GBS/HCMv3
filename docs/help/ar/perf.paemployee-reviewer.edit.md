---
helpId: perf.paemployee-reviewer.edit
lang: ar
title: مراجعو تقييم الأداء — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.PAEmployeeReviewer
surfaces:
  - angular/src/app/admin/performance-management/pa-employee-detail/pa-employee-reviewers/create-or-edit-pa-employee-reviewer-modal.component.html
audience: [admin]
permissions: []
---

## Overview

شخص يراجع التقييم -- المدير أو زميل. ويحدّ عدد المراجعين الحد الأقصى في نوع النموذج، ولا يراجع الشخص التقييم إلا مرة واحدة، ولا يمكن حذف إلا المراجعين الزملاء.

## Employee {#PAEmployeeReviewer.EmployeeId}
<!-- alias: loc:Employee, dom:PAEmployeeReviewer_EmployeeId -->

**ما هو.** المراجع.

**القواعد.** اختياري. يُختار من قائمة الموظفين.

## PeerAddedBy {#PAEmployeeReviewer.PeerAddedById}
<!-- alias: loc:PeerAddedBy, dom:PAEmployeeReviewer_PeerAddedById -->

**ما هو.** من أضاف المراجع الزميل.

**القواعد.** اختياري. يُختار من قائمة الموظفين.

## IsPeer {#PAEmployeeReviewer.IsPeer}
<!-- alias: loc:IsPeer, dom:PAEmployeeReviewer_IsPeer -->

**ما هو.** ما إذا كان المراجع زميلاً لا المدير.

**لماذا يهم.** لا يستخدم تقرير تقييم المدير إلا المراجعين غير الزملاء.

**القواعد.** نعم أو لا.

## CustomTextField1 {#PAEmployeeReviewer.CustomTextField1}
<!-- alias: loc:CustomTextField1, dom:PAEmployeeReviewer_CustomTextField1 -->

**ما هو.** نقاط قوة الموظف المُقيَّم، بكلمات المراجع.

**لماذا يهم.** تظهر نقاطَ قوة في تقرير ملخص المراجعين، من مراجعة المدير.

**القواعد.** اختياري. الحد الأقصى 4000 حرف.

## CustomTextField2 {#PAEmployeeReviewer.CustomTextField2}
<!-- alias: loc:CustomTextField2, dom:PAEmployeeReviewer_CustomTextField2 -->

**ما هو.** المجالات التي ينبغي أن يتحسن فيها الموظف المُقيَّم.

**لماذا يهم.** تظهر في تقرير ملخص المراجعين، من مراجعة المدير.

**القواعد.** اختياري. الحد الأقصى 4000 حرف.

## CustomTextField3 {#PAEmployeeReviewer.CustomTextField3}
<!-- alias: loc:CustomTextField3, dom:PAEmployeeReviewer_CustomTextField3 -->

**ما هو.** التقييم العام والاحتياجات التدريبية.

**لماذا يهم.** يظهر في تقرير ملخص المراجعين، من مراجعة المدير.

**القواعد.** اختياري. الحد الأقصى 4000 حرف.
