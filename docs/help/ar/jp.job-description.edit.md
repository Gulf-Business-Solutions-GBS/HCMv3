---
helpId: jp.job-description.edit
lang: ar
title: الأوصاف الوظيفية — إنشاء / تحرير
entity: HCMv3.JobProfileManagement.JobDescription
surfaces:
  - angular/src/app/admin/job-profile-management/job-descriptions/create-or-edit-job-description-modal.component.html
audience: [admin]
permissions: [Pages.JobProfileManagement.JobDescriptions.Create]
---

## Overview

وصف مسمى وظيفي: ملخص قصير، والنص الكامل، وقوائم بالمسؤوليات والمهارات والكفاءات والدرجات العلمية والشهادات وأسئلة المقابلة المرتبطة به. ويجب أن يكون الوصفان القصيران فريدين. يُحفظ للرجوع إليه: للوظائف الشاغرة نص وصف خاص بها ولا تستخدم هذا الوصف.

## JobTitle {#JobDescription.JobTitleId}
<!-- alias: loc:JobTitle, dom:JobDescription_JobTitleId -->

**ما هو.** المسمى الوظيفي الموصوف.

**مثال.** مسؤول موارد بشرية

**القواعد.** إلزامي. يُختار من قائمة المسميات الوظيفية.

## Version {#JobDescription.Version}
<!-- alias: loc:Version, dom:JobDescription_Version -->

**ما هو.** إصدار الوصف.

**مثال.** 2.0

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## VersionChangedOn {#JobDescription.VersionChangedOn}
<!-- alias: loc:VersionChangedOn, dom:JobDescription_VersionChangedOn -->

**ما هو.** متى أصبح هذا الإصدار سارياً.

**القواعد.** اختياري. تاريخ.

## ShortDescriptionEn {#JobDescription.ShortDescriptionEn}
<!-- alias: loc:ShortDescriptionEn, dom:JobDescription_ShortDescriptionEn -->

**ما هو.** ملخص في سطر واحد بالإنجليزية.

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## ShortDescriptionAr {#JobDescription.ShortDescriptionAr}
<!-- alias: loc:ShortDescriptionAr, dom:JobDescription_ShortDescriptionAr -->

**ما هو.** ملخص في سطر واحد بالعربية.

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## LongDescriptionEn {#JobDescription.LongDescriptionEn}
<!-- alias: loc:LongDescriptionEn, dom:JobDescription_LongDescriptionEn -->

**ما هو.** الوصف الكامل بالإنجليزية.

**القواعد.** إلزامي.

## LongDescriptionAr {#JobDescription.LongDescriptionAr}
<!-- alias: loc:LongDescriptionAr, dom:JobDescription_LongDescriptionAr -->

**ما هو.** الوصف الكامل بالعربية.

**القواعد.** إلزامي.
