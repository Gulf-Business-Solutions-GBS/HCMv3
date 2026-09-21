---
helpId: jp.job-description-acknowledgement.edit
lang: ar
title: إقرارات الوصف الوظيفي — إنشاء / تحرير
entity: HCMv3.JobProfileManagement.JobDescriptionAcknowledgement
surfaces:
  - angular/src/app/admin/job-profile-management/job-description-acknowledgements/create-or-edit-job-description-acknowledgement-modal.component.html
audience: [admin]
permissions: [Pages.JobProfileManagement.JobDescriptionAcknowledgements.Create]
---

## Overview

سجل بأن موظفاً قرأ وصفاً وظيفياً وقبله. ولكل موظف إقرار واحد لكل مسمى وظيفي ووصف وظيفي، فيُسجَّل الإصدار الجديد بتعديل الإقرار الموجود.

## Employee {#JobDescriptionAcknowledgement.EmployeeId}
<!-- alias: loc:Employee, dom:JobDescriptionAcknowledgement_EmployeeId -->

**ما هو.** الموظف.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## JobTitle {#JobDescriptionAcknowledgement.JobTitleId}
<!-- alias: loc:JobTitle, dom:JobDescriptionAcknowledgement_JobTitleId -->

**ما هو.** المسمى الوظيفي.

**القواعد.** إلزامي. يُختار من قائمة المسميات الوظيفية.

## JobDescription {#JobDescriptionAcknowledgement.JobDescriptionId}
<!-- alias: loc:JobDescription, dom:JobDescriptionAcknowledgement_JobDescriptionId -->

**ما هو.** الوصف الوظيفي الذي أُقرّ به.

**القواعد.** إلزامي. يُختار من قائمة الأوصاف الوظيفية.

## Version {#JobDescriptionAcknowledgement.Version}
<!-- alias: loc:Version, dom:JobDescriptionAcknowledgement_Version -->

**ما هو.** الإصدار الذي أُقرّ به.

**مثال.** 2.0

**لماذا يهم.** يُكتب يدوياً، ولا يُنسخ من الوصف الوظيفي.

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## Acknowledged {#JobDescriptionAcknowledgement.Acknowledged}
<!-- alias: loc:Acknowledged, dom:JobDescriptionAcknowledgement_Acknowledged -->

**ما هو.** ما إذا كان الموظف قد أقرّ به.

**القواعد.** نعم أو لا.
