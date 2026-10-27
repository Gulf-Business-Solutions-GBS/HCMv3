---
helpId: pa.employee-certification.edit
lang: ar
title: شهادات الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeCertification
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/certifications/create-or-edit-employee-certification-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeCertifications.Create]
---

## Overview

دورة أو شهادة مهنية أنهاها الموظف.

تُسجَّل للرجوع إليها، ولا يقرؤها أي احتساب أو تقرير حالياً. ويمكن ربط الشهادات التدريبية من سجلات تدريب الموظف.

## Course {#EmployeeCertification.Course}
<!-- alias: loc:Course, dom:EmployeeCertification_Course -->

**ما هو.** اسم الدورة.

**مثال.** أخصائي رواتب معتمد

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## Institution {#EmployeeCertification.InstitutionId}
<!-- alias: loc:Institution, dom:EmployeeCertification_InstitutionId -->

**ما هو.** الجهة التي نظمت الدورة.

**مثال.** معهد الكويت للأبحاث العلمية

**القواعد.** إلزامي. يُختار من قائمة الجهات التعليمية.

## Duration {#EmployeeCertification.Duration}
<!-- alias: loc:Duration, dom:EmployeeCertification_Duration -->

**ما هو.** مدة الدورة، كنص حر.

**مثال.** 3 أشهر

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## Certificate {#EmployeeCertification.Certificate}
<!-- alias: loc:Certificate, dom:EmployeeCertification_Certificate -->

**ما هو.** الشهادة الممنوحة.

**مثال.** CPP

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## Grade {#EmployeeCertification.Grade}
<!-- alias: loc:Grade, dom:EmployeeCertification_Grade -->

**ما هو.** التقدير الحاصل عليه.

**مثال.** ناجح

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## CertificationType {#EmployeeCertification.CertificationType}
<!-- alias: loc:CertificationType, dom:EmployeeCertification_CertificationType -->

**ما هو.** ما إذا كانت هذه شهادة مهنية أم شهادة نالها الموظف من خلال التدريب.

**مثال.** تدريبية

**لماذا يهم.** لا يمكن ربط إلا الشهادات التدريبية من سجلات تدريب الموظف. والسجلات التي كانت موجودة قبل إضافة هذا الحقل مهنية.

**القواعد.** إلزامي. إحدى القيم: مهنية، تدريبية.
