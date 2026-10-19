---
helpId: trv.travel-feedback.edit
lang: ar
title: تقييمات السفر — إنشاء / تحرير
entity: HCMv3.TravelManagement.TravelFeedback
surfaces:
  - angular/src/app/admin/travel-management/travel-feedbacks/create-or-edit-travel-feedback-modal.component.html
audience: [admin]
permissions: [Pages.TravelManagement.TravelFeedbacks.Create]
---

## Overview

مطالبة بمصروفات سفر. المطالبات التي يقدمها الموظف في الخدمة الذاتية تذهب إلى الموافقة: يجب أن يكون المبلغ أكبر من 0، ولا يجوز أن تكون للموظف مطالبة أخرى للرحلة نفسها، إلا إذا رُفضت أو أُعيدت. ولا تُدفع المطالبات عبر الرواتب.

يحدد خيار **موظف أو متقدم لوظيفة** في أعلى النموذج صاحب المطالبة: موظف أو متقدم لوظيفة. والمطالبة تخص أحدهما فقط، ولا تُعرض في حقل **السفر** إلا رحلات هذا الشخص، وعند التبديل يُمسح الشخص الآخر والرحلة.

مطالبة الموظف المُدخلة هنا تذهب إلى الموافقة إذا كانت مطالبات السفر مُعدّة للاعتماد، وتُحفظ مباشرة إذا لم تكن كذلك. أما مطالبة المتقدم لوظيفة فتُحفظ مباشرة دائماً، لأن مسار الموافقة يُحدَّد بالموظف الذي يخصه الطلب، والمتقدم لا مدير له ولا وحدة يُوجَّه الطلب من خلالها. والمطالبة التي أُرسلت للاعتماد لا يمكن نقلها لاحقاً من الموظف إلى متقدم لوظيفة.

## Employee {#TravelFeedback.EmployeeId}
<!-- alias: loc:Employee, dom:TravelFeedback_EmployeeId -->

**ما هو.** الموظف المطالِب.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## Applicant {#TravelFeedback.ApplicantId}
<!-- alias: loc:Applicant, dom:TravelFeedback_ApplicantId -->

**ما هو.** المتقدم لوظيفة صاحب المطالبة. يظهر بدلاً من حقل الموظف عند اختيار **المتقدم** في الأعلى.

**مثال.** APP-0042 - سارة أحمد

**لماذا يهم.** اختيار المتقدم يقصر قائمة **السفر** على رحلاته. وتُحفظ المطالبة فوراً دون موافقة.

**القواعد.** إلزامي. يُختار من قائمة المتقدمون.

## Travel {#TravelFeedback.TravelId}
<!-- alias: loc:Travel, dom:TravelFeedback_TravelId -->

**ما هو.** الرحلة التي تخصها المطالبة، إن وُجدت.

**القواعد.** اختياري. يُختار من قائمة السفريات.

## ClaimDate {#TravelFeedback.ClaimDate}
<!-- alias: loc:ClaimDate, dom:TravelFeedback_ClaimDate -->

**ما هو.** متى قُدمت المطالبة.

**القواعد.** إلزامي. تاريخ.

## ClaimAmount {#TravelFeedback.ClaimAmount}
<!-- alias: loc:ClaimAmount, dom:TravelFeedback_ClaimAmount -->

**ما هو.** المبلغ المطالب به.

**مثال.** 75.500

**القواعد.** إلزامي. رقم.
