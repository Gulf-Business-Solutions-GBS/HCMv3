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

مطالبة بمصروفات سفر. المطالبات التي يقدمها الموظف في الخدمة الذاتية تذهب إلى الموافقة: يجب أن يكون المبلغ أكبر من 0، ولا يجوز أن تكون للموظف مطالبة أخرى للرحلة نفسها، إلا إذا رُفضت أو أُعيدت. أما المطالبة المُدخلة هنا فتُحفظ مباشرة. ولا تُدفع المطالبات عبر الرواتب.

## Employee {#TravelFeedback.EmployeeId}
<!-- alias: loc:Employee, dom:TravelFeedback_EmployeeId -->

**ما هو.** الموظف المطالِب.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

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
