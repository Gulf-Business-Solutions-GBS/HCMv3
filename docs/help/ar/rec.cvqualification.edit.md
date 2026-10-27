---
helpId: rec.cvqualification.edit
lang: ar
title: مؤهلات السيرة الذاتية — إنشاء / تحرير
entity: HCMv3.Recruitment.CVQualification
surfaces:
  - angular/src/app/admin/recruitment/applicant-detail/cv/qualifications/create-or-edit-cv-qualification-modal.component.html
audience: [admin]
permissions: [Pages.Recruitment.CVQualifications.Create]
---

## Overview

مؤهل في السيرة الذاتية.

## Qualification {#CVQualification.QualificationId}
<!-- alias: loc:Qualification, dom:CVQualification_QualificationId -->

**ما هو.** المؤهل.

**مثال.** بكالوريوس

**القواعد.** اختياري. يُختار من قائمة المؤهلات.

## University {#CVQualification.University}
<!-- alias: loc:University, dom:CVQualification_University -->

**ما هو.** الجهة التي مُنح منها.

**مثال.** جامعة الكويت

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## Percentage {#CVQualification.Percentage}
<!-- alias: loc:Percentage, dom:CVQualification_Percentage -->

**ما هو.** الدرجة، نسبةً مئوية.

**مثال.** `82`

**القواعد.** اختياري. رقم.

## PassedYear {#CVQualification.PassedYear}
<!-- alias: loc:PassedYear, dom:CVQualification_PassedYear -->

**ما هو.** سنة الحصول عليه.

**مثال.** `2018`

**القواعد.** اختياري. رقم صحيح.
