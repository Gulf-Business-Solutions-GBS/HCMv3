---
helpId: rec.applicant-cv.edit
lang: ar
title: السير الذاتية للمتقدمين — إنشاء / تحرير
entity: HCMv3.Recruitment.ApplicantCV
surfaces:
  - angular/src/app/admin/recruitment/applicant-detail/cv/applicant-cv.component.html
audience: [admin]
permissions: []
---

## Overview

إحدى السير الذاتية للمتقدم. يمكن للمرشح الاحتفاظ بعدة سير ذاتية في موقع الوظائف ويختار إحداها عند التقديم. وتُحفظ لغاتها ومؤهلاتها ومهاراتها وخبراتها في تفاصيلها. ويجب أن تختلف أسماء السير الذاتية للمتقدم الواحد، ولا يستطيع المرشح تعديل السيرة التي لها طلب تجاوز مرحلة *تم التقديم*.

## JobTitle {#ApplicantCV.JobTitleId}
<!-- alias: loc:JobTitle, dom:ApplicantCV_JobTitleId -->

**ما هو.** المسمى الوظيفي الذي تستهدفه السيرة.

**مثال.** محاسب

**القواعد.** اختياري. يُختار من قائمة المسميات الوظيفية.

## CvName {#ApplicantCV.CVName}
<!-- alias: loc:CvName, dom:ApplicantCV_CvName -->

**ما هو.** اسم للسيرة الذاتية لتمييزها عن غيرها لدى المتقدم.

**مثال.** وظائف المالية

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## UploadDate {#ApplicantCV.UploadDate}
<!-- alias: loc:UploadDate, dom:ApplicantCV_UploadDate -->

**ما هو.** تاريخ إضافة السيرة. ويسرد موقع الوظائف الأحدث أولاً.

**القواعد.** اختياري. تاريخ.
