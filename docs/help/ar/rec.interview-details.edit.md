---
helpId: rec.interview-details.edit
lang: ar
title: تفاصيل المقابلات — إنشاء / تحرير
entity: HCMv3.Recruitment.InterviewDetails
surfaces:
  - angular/src/app/admin/recruitment/interview-details/create-or-edit-interview-detail-modal.component.html
audience: [admin]
permissions: []
---

## Overview

مقابلة لطلب: متى وأين ومن أجراها وماذا قيل فيها. تُحفظ مع الطلب، ولا شيء يقرؤها حالياً.

## ApplicationStatuses {#InterviewDetails.ApplicationStatusId}
<!-- alias: loc:ApplicationStatuses, dom:InterviewDetails_ApplicationStatusId -->

**ما هو.** الطلب.

**القواعد.** إلزامي. يُختار من قائمة حالات الطلبات.

## InterviewDate {#InterviewDetails.InterviewDate}
<!-- alias: loc:InterviewDate, dom:InterviewDetails_InterviewDate -->

**ما هو.** التاريخ.

**مثال.** `2026-04-20`

**القواعد.** اختياري. تاريخ.

## InterviewTime {#InterviewDetails.InterviewTime}
<!-- alias: loc:InterviewTime, dom:InterviewDetails_InterviewTime -->

**ما هو.** الوقت.

**مثال.** `10:00`

**القواعد.** اختياري. تاريخ.

## Place {#InterviewDetails.Place}
<!-- alias: loc:Place, dom:InterviewDetails_Place -->

**ما هو.** مكان إجرائها.

**مثال.** المكتب الرئيسي، الدور الثالث

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## Interviewer1 {#InterviewDetails.Interviewer1Id}
<!-- alias: loc:Interviewer1, dom:InterviewDetails_Interviewer1Id -->

**ما هو.** المُقابِل الأول.

**القواعد.** إلزامي. رقم صحيح.

## Interviewer2 {#InterviewDetails.Interviewer2Id}
<!-- alias: loc:Interviewer2, dom:InterviewDetails_Interviewer2Id -->

**ما هو.** المُقابِل الثاني.

**القواعد.** اختياري. رقم صحيح.

## Interviewer3 {#InterviewDetails.Interviewer3Id}
<!-- alias: loc:Interviewer3, dom:InterviewDetails_Interviewer3Id -->

**ما هو.** المُقابِل الثالث.

**القواعد.** اختياري. رقم صحيح.

## Comments1 {#InterviewDetails.Comments1}
<!-- alias: loc:Comments1, dom:InterviewDetails_Comments1 -->

**ما هو.** تعليقات المُقابِل الأول.

**القواعد.** اختياري.

## Comments2 {#InterviewDetails.Comments2}
<!-- alias: loc:Comments2, dom:InterviewDetails_Comments2 -->

**ما هو.** تعليقات المُقابِل الثاني.

**القواعد.** اختياري.

## Comments3 {#InterviewDetails.Comments3}
<!-- alias: loc:Comments3, dom:InterviewDetails_Comments3 -->

**ما هو.** تعليقات المُقابِل الثالث.

**القواعد.** اختياري.
