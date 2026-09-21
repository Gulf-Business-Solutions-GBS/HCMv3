---
helpId: perf.paform.edit
lang: ar
title: نماذج تقييم الأداء — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.PAForm
surfaces:
  - angular/src/app/admin/performance-management/pa-forms/create-or-edit-pa-form-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.PAForms.Create]
---

## Overview

نموذج تقييم الموظف لفترة، في النموذج القديم: الخطة والمراجعات الربعية ومراجعتا منتصف السنة ونهايتها، مع الاعتماد النهائي والتعليقات الختامية. وتُحفظ أسطر مؤشراته في قوائم الخطة والمراجعات. ولكل موظف نموذج واحد لكل فترة.

وتسجّل خانات التقديم مدى التقدم ولا تقفل النموذج. وتعدّ لوحة المعلومات تقديمات الخطة ومنتصف السنة ونهايتها، ويعرض تقرير ملخص التقييم الدرجة بعد التعديل والتعليقات.

## PAPeriod {#PAForm.PAPeriodId}
<!-- alias: loc:PAPeriod, dom:PAForm_PAPeriodId -->

**ما هو.** فترة التقييم.

**مثال.** السنة المالية 2026

**القواعد.** إلزامي. يُختار من قائمة فترات تقييم الأداء.

## Employee {#PAForm.EmployeeId}
<!-- alias: loc:Employee, dom:PAForm_EmployeeId -->

**ما هو.** الموظف المُقيَّم.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## ModeratedScore {#PAForm.ModeratedScore}
<!-- alias: loc:ModeratedScore, dom:PAForm_ModeratedScore -->

**ما هو.** الدرجة النهائية بعد التعديل، وتظهر في تقريري ملخص التقييم وتقييم الأداء.

**مثال.** `78`

**القواعد.** اختياري. رقم.

## PlanSubmitByAppraiser {#PAForm.PlanSubmitByAppraiser}
<!-- alias: loc:PlanSubmitByAppraiser, dom:PAForm_PlanSubmitByAppraiser -->

**ما هو.** ما إذا كان المُقيِّم قد قدّم الخطة.

**لماذا يهم.** يُعدّ في لوحة معلومات تقييم الأداء.

**القواعد.** نعم أو لا.

## PlanSubmitByAppraisee {#PAForm.PlanSubmitByAppraisee}
<!-- alias: loc:PlanSubmitByAppraisee, dom:PAForm_PlanSubmitByAppraisee -->

**ما هو.** ما إذا كان الموظف المُقيَّم قد قدّم الخطة.

**لماذا يهم.** يُعدّ في لوحة معلومات تقييم الأداء.

**القواعد.** نعم أو لا.

## Quarter1SubmitByAppraiser {#PAForm.Quarter1SubmitByAppraiser}
<!-- alias: loc:Quarter1SubmitByAppraiser, dom:PAForm_Quarter1SubmitByAppraiser -->

**ما هو.** ما إذا كان المُقيِّم قد قدّم مراجعة الربع الأول.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## Quarter1SubmitByAppraisee {#PAForm.Quarter1SubmitByAppraisee}
<!-- alias: loc:Quarter1SubmitByAppraisee, dom:PAForm_Quarter1SubmitByAppraisee -->

**ما هو.** ما إذا كان الموظف المُقيَّم قد قدّم مراجعة الربع الأول.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## MRSubmitByAppraiser {#PAForm.MRSubmitByAppraiser}
<!-- alias: loc:MRSubmitByAppraiser, dom:PAForm_MRSubmitByAppraiser -->

**ما هو.** ما إذا كان المُقيِّم قد قدّم مراجعة منتصف السنة.

**لماذا يهم.** يُعدّ في لوحة معلومات تقييم الأداء.

**القواعد.** نعم أو لا.

## MRSubmitByAppraisee {#PAForm.MRSubmitByAppraisee}
<!-- alias: loc:MRSubmitByAppraisee, dom:PAForm_MRSubmitByAppraisee -->

**ما هو.** ما إذا كان الموظف المُقيَّم قد قدّم مراجعة منتصف السنة.

**لماذا يهم.** يُعدّ في لوحة معلومات تقييم الأداء.

**القواعد.** نعم أو لا.

## Quarter3SubmitByAppraiser {#PAForm.Quarter3SubmitByAppraiser}
<!-- alias: loc:Quarter3SubmitByAppraiser, dom:PAForm_Quarter3SubmitByAppraiser -->

**ما هو.** ما إذا كان المُقيِّم قد قدّم مراجعة الربع الثالث.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## Quarter3SubmitByAppraisee {#PAForm.Quarter3SubmitByAppraisee}
<!-- alias: loc:Quarter3SubmitByAppraisee, dom:PAForm_Quarter3SubmitByAppraisee -->

**ما هو.** ما إذا كان الموظف المُقيَّم قد قدّم مراجعة الربع الثالث.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## ERSubmitByAppraiser {#PAForm.ERSubmitByAppraiser}
<!-- alias: loc:ERSubmitByAppraiser, dom:PAForm_ERSubmitByAppraiser -->

**ما هو.** ما إذا كان المُقيِّم قد قدّم مراجعة نهاية السنة.

**لماذا يهم.** يُعدّ في لوحة معلومات تقييم الأداء.

**القواعد.** نعم أو لا.

## ERSubmitByAppraisee {#PAForm.ERSubmitByAppraisee}
<!-- alias: loc:ERSubmitByAppraisee, dom:PAForm_ERSubmitByAppraisee -->

**ما هو.** ما إذا كان الموظف المُقيَّم قد قدّم مراجعة نهاية السنة.

**لماذا يهم.** يُعدّ في لوحة معلومات تقييم الأداء.

**القواعد.** نعم أو لا.

## AppraiserSignedOff {#PAForm.AppraiserSignedOff}
<!-- alias: loc:AppraiserSignedOff, dom:PAForm_AppraiserSignedOff -->

**ما هو.** ما إذا كان المُقيِّم قد اعتمد النموذج نهائياً.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## AppraiseeSignedOff {#PAForm.AppraiseeSignedOff}
<!-- alias: loc:AppraiseeSignedOff, dom:PAForm_AppraiseeSignedOff -->

**ما هو.** ما إذا كان الموظف المُقيَّم قد اعتمد النموذج نهائياً.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## AppraiseeStrength {#PAForm.AppraiseeStrength}
<!-- alias: loc:AppraiseeStrength, dom:PAForm_AppraiseeStrength -->

**ما هو.** نقاط قوة الموظف المُقيَّم. تظهر في تقرير ملخص التقييم.

**القواعد.** اختياري.

## AreasOfImprovement {#PAForm.AreasOfImprovement}
<!-- alias: loc:AreasOfImprovement, dom:PAForm_AreasOfImprovement -->

**ما هو.** مجالات التحسين. تظهر في تقرير ملخص التقييم.

**القواعد.** اختياري.

## ReviewofPreviousGoals {#PAForm.ReviewofPreviousGoals}
<!-- alias: loc:ReviewofPreviousGoals, dom:PAForm_ReviewofPreviousGoals -->

**ما هو.** مراجعة أهداف الفترة السابقة.

**القواعد.** اختياري.

## NewlySetGoals {#PAForm.NewlySetGoals}
<!-- alias: loc:NewlySetGoals, dom:PAForm_NewlySetGoals -->

**ما هو.** الأهداف المحددة للفترة التالية. تظهر في تقرير ملخص التقييم.

**القواعد.** اختياري.

## OverallComments {#PAForm.OverallComments}
<!-- alias: loc:OverallComments, dom:PAForm_OverallComments -->

**ما هو.** تعليقات عامة. تظهر في تقرير ملخص التقييم.

**القواعد.** اختياري.
