---
helpId: gr.sponsor-job-title.edit
lang: ar
title: المسميات الوظيفية للكفيل — إنشاء / تحرير
entity: HCMv3.GovtRelationsManagement.SponsorJobTitle
surfaces:
  - angular/src/app/admin/govt-relations-management/sponsor-detail/job-titles/create-or-edit-sponsor-job-title-modal.component.html
audience: [admin]
permissions: [Pages.GovtRelationsManagement.SponsorJobTitles.Create]
---

## Overview

عدد الموظفين الذين يجوز أن يكونوا تحت كفيل بمسمى وظيفي حكومي واحد، مقسماً بين المواطنين وغير المواطنين. ويجب أن يكون مجموع النسبتين 100. تُحسب الأماكن المسموح بها عند الحفظ: للمواطنين عدد الوظائف مضروباً في نسبتهم مقرباً إلى الأدنى، ولغير المواطنين مقرباً إلى الأعلى. ولا يمكن إضافة سجل كفيل لموظف إلا لمسمى وظيفي له سجل لدى الكفيل. وتظهر الأماكن المتبقية في تقرير حصص المسميات الوظيفية للكفلاء، ولا يوجد ما يمنع تجاوز الحصة.

## GovtJobTitle {#SponsorJobTitle.GovtJobTitleId}
<!-- alias: loc:GovtJobTitle, dom:SponsorJobTitle_GovtJobTitleId -->

**ما هو.** المسمى الوظيفي الحكومي.

**مثال.** محاسب

**القواعد.** إلزامي. يُختار من قائمة المسميات الوظيفية الحكومية.

## NoofPosition {#SponsorJobTitle.NoofPosition}
<!-- alias: loc:NoofPosition, dom:SponsorJobTitle_NoofPosition -->

**ما هو.** إجمالي عدد الوظائف المسموح بها.

**مثال.** 20

**القواعد.** إلزامي. رقم صحيح.

## CitizenPercent {#SponsorJobTitle.CitizenPercent}
<!-- alias: loc:CitizenPercent, dom:SponsorJobTitle_CitizenPercent -->

**ما هو.** حصة المواطنين من الوظائف.

**مثال.** 30

**القواعد.** إلزامي. رقم صحيح.

## CitizenQuotaUsed {#SponsorJobTitle.CitizenQuotaUsed}
<!-- alias: loc:CitizenQuotaUsed, dom:SponsorJobTitle_CitizenQuotaUsed -->

**ما هو.** عدد الأماكن التي يشغلها مواطنون.

**لماذا يهم.** يزيد بواحد عند إضافة سجل كفيل لموظف مجموعة جنسيته "Citizen"، وينقص بواحد عند حذفه. ولا يعيد تعديل السجل العدّ.

**القواعد.** إلزامي. رقم صحيح.

## NonCitizenPercent {#SponsorJobTitle.NonCitizenPercent}
<!-- alias: loc:NonCitizenPercent, dom:SponsorJobTitle_NonCitizenPercent -->

**ما هو.** حصة غير المواطنين من الوظائف.

**مثال.** 70

**القواعد.** إلزامي. رقم صحيح.

## NonCitizenQuotaUsed {#SponsorJobTitle.NonCitizenQuotaUsed}
<!-- alias: loc:NonCitizenQuotaUsed, dom:SponsorJobTitle_NonCitizenQuotaUsed -->

**ما هو.** عدد الأماكن التي يشغلها غير مواطنين.

**لماذا يهم.** يزيد وينقص بالطريقة نفسها، للموظفين في أي مجموعة جنسية أخرى.

**القواعد.** إلزامي. رقم صحيح.
