---
helpId: sys.personnel-settings.edit
lang: ar
title: Personnel settings
entity: HCMv3.SystemSettings.ManagePersonnelSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-personnel-settings/create-or-edit-manage-personnel-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManagePersonnelSettings]
---

## Overview

إعدادات شؤون الموظفين.

لكل شركة سجل إعدادات واحد، ولا يمكن حذفه.

## ProbationDaysCalculation {#ManagePersonnelSettings.ProbationDaysCalculation}
<!-- alias: loc:ProbationDaysCalculation, dom:PersonnelSettings_ProbationDaysCalculation -->

**ما هو.** ما إذا كانت أيام التجربة تُعدّ بأيام التقويم أو بأيام العمل.

**مثال.** أيام التقويم

**لماذا يهم.** لا يُدعم إلا *أيام التقويم*: فنهاية التجربة دائماً تاريخ الالتحاق مضافاً إليه أيام التجربة. ومع *أيام العمل* ترفض أداة إعادة احتساب التجربة العمل.

**القواعد.** إلزامي. إحدى القيم: أيام التقويم، أيام العمل.

## MaxPhotoSize {#ManagePersonnelSettings.MaxPhotoSize}
<!-- alias: loc:MaxPhotoSize, dom:PersonnelSettings_MaxPhotoSize -->

**ما هو.** أكبر صورة موظف يقبلها الاستيراد الجماعي للصور، بالكيلوبايت.

**مثال.** `500`

**لماذا يهم.** القيمة 0 تستخدم القيمة الافتراضية للاستيراد. ولا يحدّ الصور المختارة في نموذج الموظف.

**القواعد.** اختياري. رقم صحيح.

## EnableSponsorGovtJobTitleQuota {#ManagePersonnelSettings.EnableSponsorGovtJobTitleQuota}
<!-- alias: loc:EnableSponsorGovtJobTitleQuota, dom:PersonnelSettings_EnableSponsorGovtJobTitleQuota -->

**ما هو.** ما إذا كانت حصص المسميات الوظيفية للكفلاء تُحتسب.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## AutoWageAssignOnHire {#ManagePersonnelSettings.AutoWageAssignOnHire}
<!-- alias: loc:AutoWageAssignOnHire, dom:PersonnelSettings_AutoWageAssignOnHire -->

**ما هو.** ما إذا كانت أنواع الأجور تُعيَّن تلقائياً عند تعيين موظف.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.
